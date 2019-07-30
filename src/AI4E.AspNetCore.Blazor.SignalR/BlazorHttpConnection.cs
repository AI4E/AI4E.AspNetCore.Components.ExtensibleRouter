/* License
 * --------------------------------------------------------------------------------------------------------------------
 * This file is part of the AI4E distribution.
 *   (https://github.com/AI4E/AI4E.AspNetCore.Components.Extensions)
 *
 * MIT License
 *
 * Copyright (c) 2019 Andreas Truetschel and contributors.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * --------------------------------------------------------------------------------------------------------------------
 */

/* Based on
 * --------------------------------------------------------------------------------------------------------------------
 * BlazorSignalR (https://github.com/csnewman/BlazorSignalR)
 *
 * MIT License
 *
 * Copyright (c) 2018 csnewman
 * --------------------------------------------------------------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Blazor.Http;
using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace AI4E.AspNetCore.Blazor.SignalR
{
    internal class BlazorHttpConnection : ConnectionContext, IConnectionInherentKeepAliveFeature
    {
        private static readonly int _maxRedirects = 100;
        private static readonly Task<string> NoAccessToken = Task.FromResult<string>(null);
        private static readonly TimeSpan HttpClientTimeout = TimeSpan.FromSeconds(120.0);

        public override string ConnectionId
        {
            get => _connectionId;
            set => throw new InvalidOperationException(
                "The ConnectionId is set internally and should not be set by user code.");
        }

        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override IDictionary<object, object> Items { get; set; } = new ConnectionItems();

        public override IDuplexPipe Transport
        {
            get
            {
                CheckDisposed();

                if (_transport == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot access the {nameof(Transport)} pipe before the connection has started.");
                }

                return _transport;
            }
            set => throw new NotSupportedException("The transport pipe isn't settable.");
        }

        bool IConnectionInherentKeepAliveFeature.HasInherentKeepAlive => _hasInherentKeepAlive;

        private readonly BlazorHttpConnectionOptions _options;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<BlazorHttpConnection> _logger;
        private readonly HttpClient _httpClient;

        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);

        private string _connectionId;
        private IDuplexPipe _transport;
        private bool _disposed;
        private bool _started;
        private bool _hasInherentKeepAlive;
        private Func<Task<string>> _accessTokenProvider;

        public BlazorHttpConnection(BlazorHttpConnectionOptions options, IJSRuntime jsRuntime, ILoggerFactory loggerFactory)
        {
            if (jsRuntime == null)
                throw new ArgumentNullException(nameof(jsRuntime));

            _options = options;
            _jsRuntime = jsRuntime;
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _logger = _loggerFactory.CreateLogger<BlazorHttpConnection>();
            _httpClient = CreateHttpClient();
            Features.Set<IConnectionInherentKeepAliveFeature>(this);
        }

        public Task StartAsync()
        {
            return StartAsync(_options.DefaultTransferFormat);
        }

        public async Task StartAsync(TransferFormat transferFormat)
        {
            CheckDisposed();

            if (_started)
            {
                Log.SkippingStart(_logger);
                return;
            }

            await _connectionLock.WaitAsync();

            try
            {
                CheckDisposed();
                if (_started)
                {
                    Log.SkippingStart(_logger);
                }
                else
                {
                    Log.Starting(_logger);
                    await SelectAndStartTransport(transferFormat);
                    _started = true;
                    Log.Started(_logger);
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        private async Task SelectAndStartTransport(TransferFormat transferFormat)
        {
            var uri = _options.Url;

            // Fix relative url paths
            if (!uri.IsAbsoluteUri || uri.Scheme == Uri.UriSchemeFile && uri.OriginalString.StartsWith("/", StringComparison.Ordinal))
            {
                var baseUrl = new Uri(WebAssemblyUriHelper.Instance.GetBaseUri());
                uri = new Uri(baseUrl, uri);
            }

            _accessTokenProvider = _options.AccessTokenProvider;
            if (_options.SkipNegotiation)
            {
                if (_options.Transports != HttpTransportType.WebSockets)
                    throw new InvalidOperationException(
                        "Negotiation can only be skipped when using the WebSocket transport directly.");
                Log.StartingTransport(_logger, _options.Transports, uri);
                await StartTransport(uri, _options.Transports, transferFormat);
            }
            else
            {
                var redirects = 0;
                NegotiationResponse negotiationResponse;
                do
                {
                    negotiationResponse = await GetNegotiationResponseAsync(uri);
                    if (negotiationResponse.Url != null)
                        uri = new Uri(negotiationResponse.Url);
                    if (negotiationResponse.AccessToken != null)
                    {
                        var accessToken = negotiationResponse.AccessToken;
                        _accessTokenProvider = () => Task.FromResult(accessToken);
                    }

                    ++redirects;
                }
                while (negotiationResponse.Url != null && redirects < _maxRedirects);

                if (redirects == _maxRedirects && negotiationResponse.Url != null)
                {
                    throw new InvalidOperationException("Negotiate redirection limit exceeded.");
                }

                var connectUrl = CreateConnectUrl(uri, negotiationResponse.ConnectionId);
                var transferFormatString = transferFormat.ToString();

                foreach (var current in negotiationResponse.AvailableTransports)
                {
                    if (!Enum.TryParse(current.Transport, out HttpTransportType transportType))
                    {
                        Log.TransportNotSupported(_logger, current.Transport);
                    }
                    else
                    {
                        try
                        {
                            if ((transportType & _options.Transports) == HttpTransportType.None)
                            {
                                Log.TransportDisabledByClient(_logger, transportType);
                            }
                            else if (!current.TransferFormats.Contains(transferFormatString, StringComparer.Ordinal))
                            {
                                Log.TransportDoesNotSupportTransferFormat(_logger, transportType, transferFormat);
                            }
                            else
                            {
                                if (negotiationResponse == null)
                                {
                                    connectUrl = CreateConnectUrl(uri, (await GetNegotiationResponseAsync(uri)).ConnectionId);
                                }

                                Log.StartingTransport(_logger, transportType, connectUrl);
                                await StartTransport(connectUrl, transportType, transferFormat);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.TransportFailed(_logger, transportType, ex);
                            negotiationResponse = null;
                        }
                    }
                }
            }

            if (_transport == null)
            {
                throw new InvalidOperationException(
                    "Unable to connect to the server with any of the available transports.");
            }

        }

        private async Task StartTransport(Uri connectUrl, HttpTransportType transportType,
            TransferFormat transferFormat)
        {
            var transport = await CreateTransport(transportType);
            try
            {
                await transport.StartAsync(connectUrl, transferFormat);
            }
            catch (Exception ex)
            {
                Log.ErrorStartingTransport(_logger, transportType, ex);
                _transport = null;
                throw;
            }

            _hasInherentKeepAlive = transportType == HttpTransportType.LongPolling;
            _transport = transport;
            Log.TransportStarted(_logger, transportType);
        }

        private async Task<IDuplexPipe> CreateTransport(HttpTransportType availableServerTransports)
        {
            var useWebSockets = (availableServerTransports & HttpTransportType.WebSockets & _options.Transports) ==
                                 HttpTransportType.WebSockets;

            if (useWebSockets && (_options.Implementations & BlazorTransportType.ManagedWebSockets) ==
                BlazorTransportType.ManagedWebSockets)
            {
                // TODO: Add C# websocket implementation
                //                    return (ITransport) new WebSocketsTransport(this._httpConnectionOptions, this._loggerFactory,
                //                            this._accessTokenProvider);
                //                throw new NotImplementedException("Websocket support has not been implemented!");
            }

            if (useWebSockets && (_options.Implementations & BlazorTransportType.JsWebSockets) ==
                BlazorTransportType.JsWebSockets && await BlazorWebSocketsTransport.IsSupportedAsync(_jsRuntime))
            {
                return new BlazorWebSocketsTransport(await GetAccessTokenAsync(), _jsRuntime, _loggerFactory);
            }

            var useSSE = (availableServerTransports & HttpTransportType.ServerSentEvents & _options.Transports) ==
                          HttpTransportType.ServerSentEvents;

            if (useSSE && (_options.Implementations & BlazorTransportType.JsServerSentEvents) ==
                BlazorTransportType.JsServerSentEvents && await BlazorServerSentEventsTransport.IsSupportedAsync(_jsRuntime))
            {
                return new BlazorServerSentEventsTransport(await GetAccessTokenAsync(), _httpClient, _jsRuntime, _loggerFactory);
            }

            if (useSSE && (_options.Implementations & BlazorTransportType.ManagedServerSentEvents) ==
                BlazorTransportType.ManagedServerSentEvents && false)
            {
                return (IDuplexPipe)ReflectionHelper.CreateInstance(typeof(HttpConnection).Assembly, "Microsoft.AspNetCore.Http.Connections.Client.Internal.ServerSentEventsTransport", _httpClient, _loggerFactory);
            }

            var useLongPolling = (availableServerTransports & HttpTransportType.LongPolling & _options.Transports) ==
                                  HttpTransportType.LongPolling;

            if (useLongPolling && (_options.Implementations & BlazorTransportType.JsLongPolling) ==
                BlazorTransportType.JsLongPolling)
            {
                // TODO: Add JS long polling implementation
            }

            if (useLongPolling && (_options.Implementations & BlazorTransportType.ManagedLongPolling) ==
                BlazorTransportType.ManagedLongPolling)
            {
                return (IDuplexPipe)ReflectionHelper.CreateInstance(typeof(HttpConnection).Assembly, "Microsoft.AspNetCore.Http.Connections.Client.Internal.LongPollingTransport", _httpClient, _loggerFactory);
            }

            throw new InvalidOperationException(
                "No requested transports available on the server (and are enabled locally).");
        }

        private async Task<NegotiationResponse> GetNegotiationResponseAsync(Uri uri)
        {
            var negotiationResponse = await NegotiateAsync(uri, _httpClient, _logger);
            _connectionId = negotiationResponse.ConnectionId;
            return negotiationResponse;
        }

        private async Task<NegotiationResponse> NegotiateAsync(Uri url, HttpClient httpClient, ILogger logger)
        {
            NegotiationResponse negotiationResponse;
            try
            {
                Log.EstablishingConnection(logger, url);
                var uriBuilder = new UriBuilder(url);

                if (!uriBuilder.Path.EndsWith("/"))
                {
                    uriBuilder.Path += "/";
                }

                uriBuilder.Path += "negotiate";
                using var request = new HttpRequestMessage(HttpMethod.Post, uriBuilder.Uri)
                {
                    Version = new Version(1, 1)
                };

                using var response1 = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response1.EnsureSuccessStatusCode();
                NegotiationResponse response2;
                var content = await response1.Content.ReadAsByteArrayAsync();
                response2 = NegotiateProtocol.ParseResponse(content);
                Log.ConnectionEstablished(_logger, response2.ConnectionId);
                negotiationResponse = response2;
            }
            catch (Exception ex)
            {
                Log.ErrorWithNegotiation(logger, url, ex);
                throw;
            }

            return negotiationResponse;
        }

        private static Uri CreateConnectUrl(Uri url, string connectionId)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new FormatException("Invalid connection id.");

            return AppendQueryString(url, "id=" + connectionId);
        }

        private static Uri AppendQueryString(Uri url, string qs)
        {
            if (string.IsNullOrEmpty(qs))
                return url;
            var uriBuilder = new UriBuilder(url);
            var query = uriBuilder.Query;
            if (!string.IsNullOrEmpty(uriBuilder.Query))
                query += "&";
            var str = query + qs;
            if (str.Length > 0 && str[0] == '?')
                str = str.Substring(1);
            uriBuilder.Query = str;
            return uriBuilder.Uri;
        }

        private HttpClient CreateHttpClient()
        {
            HttpMessageHandler handler = new WebAssemblyHttpMessageHandler();

            if (_options.HttpMessageHandlerFactory != null)
            {
                handler = _options.HttpMessageHandlerFactory(handler);

                if (handler == null)
                    throw new InvalidOperationException("Configured HttpMessageHandlerFactory did not return a value.");
            }

            handler = new BlazorAccessTokenHttpMessageHandler(handler, this);

            var httpClient = new HttpClient(new LoggingHttpMessageHandler(handler, _loggerFactory))
            {
                BaseAddress = new Uri(WebAssemblyUriHelper.Instance.GetBaseUri()),
                Timeout = HttpClientTimeout
            };
            //            httpClient.DefaultRequestHeaders.UserAgent.Add(Constants.UserAgentHeader);
            if (_options.Headers != null)
            {
                foreach (var header in _options.Headers)
                {
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            httpClient.DefaultRequestHeaders.Remove("X-Requested-With");
            httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            return httpClient;
        }

        internal Task<string> GetAccessTokenAsync()
        {
            return _accessTokenProvider == null ? NoAccessToken : _accessTokenProvider();
        }

        public override async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            await _connectionLock.WaitAsync();

            try
            {
                if (!_disposed && _started)
                {
                    Log.DisposingHttpConnection(_logger);
                    try
                    {
                        await _transport.StopAsync();
                    }
                    catch (Exception ex)
                    {
                        Log.TransportThrewExceptionOnStop(_logger, ex);
                    }

                    Log.Disposed(_logger);
                }
                else
                    Log.SkippingDispose(_logger);
            }
            finally
            {
                _disposed = true;
                _connectionLock.Release();
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BlazorHttpConnection));
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _starting =
                LoggerMessage.Define(LogLevel.Debug, new EventId(1, "Starting"), "Starting HttpConnection.");

            private static readonly Action<ILogger, Exception> _skippingStart = LoggerMessage.Define(LogLevel.Debug,
                new EventId(2, "SkippingStart"), "Skipping start, connection is already started.");

            private static readonly Action<ILogger, Exception> _started = LoggerMessage.Define(LogLevel.Information,
                new EventId(3, "Started"), "HttpConnection Started.");

            private static readonly Action<ILogger, Exception> _disposingHttpConnection =
                LoggerMessage.Define(LogLevel.Debug, new EventId(4, "DisposingHttpConnection"),
                    "Disposing HttpConnection.");

            private static readonly Action<ILogger, Exception> _skippingDispose = LoggerMessage.Define(LogLevel.Debug,
                new EventId(5, "SkippingDispose"), "Skipping dispose, connection is already disposed.");

            private static readonly Action<ILogger, Exception> _disposed = LoggerMessage.Define(LogLevel.Information,
                new EventId(6, "Disposed"), "HttpConnection Disposed.");

            private static readonly Action<ILogger, string, Uri, Exception> _startingTransport =
                LoggerMessage.Define<string, Uri>(LogLevel.Debug, new EventId(7, "StartingTransport"),
                    "Starting transport '{Transport}' with Url: {Url}.");

            private static readonly Action<ILogger, Uri, Exception> _establishingConnection =
                LoggerMessage.Define<Uri>(LogLevel.Debug, new EventId(8, "EstablishingConnection"),
                    "Establishing connection with server at '{Url}'.");

            private static readonly Action<ILogger, string, Exception> _connectionEstablished =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(9, "Established"),
                    "Established connection '{ConnectionId}' with the server.");

            private static readonly Action<ILogger, Uri, Exception> _errorWithNegotiation =
                LoggerMessage.Define<Uri>(LogLevel.Error, new EventId(10, "ErrorWithNegotiation"),
                    "Failed to start connection. Error getting negotiation response from '{Url}'.");

            private static readonly Action<ILogger, HttpTransportType, Exception> _errorStartingTransport =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Error, new EventId(11, "ErrorStartingTransport"),
                    "Failed to start connection. Error starting transport '{Transport}'.");

            private static readonly Action<ILogger, string, Exception> _transportNotSupported =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(12, "TransportNotSupported"),
                    "Skipping transport {TransportName} because it is not supported by this client.");

            private static readonly Action<ILogger, string, string, Exception> _transportDoesNotSupportTransferFormat =
                LoggerMessage.Define<string, string>(LogLevel.Debug,
                    new EventId(13, "TransportDoesNotSupportTransferFormat"),
                    "Skipping transport {TransportName} because it does not support the requested transfer format '{TransferFormat}'.");

            private static readonly Action<ILogger, string, Exception> _transportDisabledByClient =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(14, "TransportDisabledByClient"),
                    "Skipping transport {TransportName} because it was disabled by the client.");

            private static readonly Action<ILogger, string, Exception> _transportFailed =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(15, "TransportFailed"),
                    "Skipping transport {TransportName} because it failed to initialize.");

            private static readonly Action<ILogger, Exception> _webSocketsNotSupportedByOperatingSystem =
                LoggerMessage.Define(LogLevel.Debug, new EventId(16, "WebSocketsNotSupportedByOperatingSystem"),
                    "Skipping WebSockets because they are not supported by the operating system.");

            private static readonly Action<ILogger, Exception> _transportThrewExceptionOnStop =
                LoggerMessage.Define(LogLevel.Error, new EventId(17, "TransportThrewExceptionOnStop"),
                    "The transport threw an exception while stopping.");

            private static readonly Action<ILogger, HttpTransportType, Exception> _transportStarted =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Debug, new EventId(18, "TransportStarted"),
                    "Transport '{Transport}' started.");

            public static void Starting(ILogger logger)
            {
                _starting(logger, null);
            }

            public static void SkippingStart(ILogger logger)
            {
                _skippingStart(logger, null);
            }

            public static void Started(ILogger logger)
            {
                _started(logger, null);
            }

            public static void DisposingHttpConnection(ILogger logger)
            {
                _disposingHttpConnection(logger, null);
            }

            public static void SkippingDispose(ILogger logger)
            {
                _skippingDispose(logger, null);
            }

            public static void Disposed(ILogger logger)
            {
                _disposed(logger, null);
            }

            public static void StartingTransport(ILogger logger, HttpTransportType transportType, Uri url)
            {
                if (!logger.IsEnabled(LogLevel.Debug))
                    return;
                _startingTransport(logger, transportType.ToString(), url, null);
            }

            public static void EstablishingConnection(ILogger logger, Uri url)
            {
                _establishingConnection(logger, url, null);
            }

            public static void ConnectionEstablished(ILogger logger, string connectionId)
            {
                _connectionEstablished(logger, connectionId, null);
            }

            public static void ErrorWithNegotiation(ILogger logger, Uri url, Exception exception)
            {
                _errorWithNegotiation(logger, url, exception);
            }

            public static void ErrorStartingTransport(ILogger logger, HttpTransportType transportType,
                Exception exception)
            {
                _errorStartingTransport(logger, transportType, exception);
            }

            public static void TransportNotSupported(ILogger logger, string transport)
            {
                _transportNotSupported(logger, transport, null);
            }

            public static void TransportDoesNotSupportTransferFormat(ILogger logger, HttpTransportType transport,
                TransferFormat transferFormat)
            {
                if (!logger.IsEnabled(LogLevel.Debug))
                    return;
                _transportDoesNotSupportTransferFormat(logger, transport.ToString(), transferFormat.ToString(),
                    null);
            }

            public static void TransportDisabledByClient(ILogger logger, HttpTransportType transport)
            {
                if (!logger.IsEnabled(LogLevel.Debug))
                    return;
                _transportDisabledByClient(logger, transport.ToString(), null);
            }

            public static void TransportFailed(ILogger logger, HttpTransportType transport, Exception ex)
            {
                if (!logger.IsEnabled(LogLevel.Debug))
                    return;
                _transportFailed(logger, transport.ToString(), ex);
            }

            public static void WebSocketsNotSupportedByOperatingSystem(ILogger logger)
            {
                _webSocketsNotSupportedByOperatingSystem(logger, null);
            }

            public static void TransportThrewExceptionOnStop(ILogger logger, Exception ex)
            {
                _transportThrewExceptionOnStop(logger, ex);
            }

            public static void TransportStarted(ILogger logger, HttpTransportType transportType)
            {
                _transportStarted(logger, transportType, null);
            }
        }
    }
}
