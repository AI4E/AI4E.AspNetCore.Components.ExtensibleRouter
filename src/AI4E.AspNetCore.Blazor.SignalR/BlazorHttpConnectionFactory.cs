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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace AI4E.AspNetCore.Blazor.SignalR
{
    internal class BlazorHttpConnectionFactory : IConnectionFactory
    {
        private readonly BlazorHttpConnectionOptions _options;
        private readonly IJSRuntime _jsRuntime;
        private readonly NavigationManager _navigationManager;
        private readonly ILoggerFactory _loggerFactory;

        public BlazorHttpConnectionFactory(
            IOptions<BlazorHttpConnectionOptions> options,
            IJSRuntime jsRuntime,
            NavigationManager navigationManager,
            ILoggerFactory loggerFactory)
        {
            if (jsRuntime is null)
                throw new ArgumentNullException(nameof(jsRuntime));

            if (navigationManager is null)
                throw new ArgumentNullException(nameof(navigationManager));

            _options = options.Value;
            _jsRuntime = jsRuntime;
            _navigationManager = navigationManager;
            _loggerFactory = loggerFactory;
        }

        public async ValueTask<ConnectionContext> ConnectAsync(
            EndPoint endPoint,
            CancellationToken cancellationToken = default)
        {
            if (endPoint == null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }

            if (!(endPoint is UriEndPoint uriEndPoint))
            {
                throw new NotSupportedException(
                    $"The provided {nameof(EndPoint)} must be of type {nameof(UriEndPoint)}.");
            }

            if (_options.Url != null && _options.Url != uriEndPoint.Uri)
            {
                throw new InvalidOperationException(
                    $"If {nameof(BlazorHttpConnectionOptions)}.{nameof(BlazorHttpConnectionOptions.Url)} was set, it " +
                    $"must match the {nameof(UriEndPoint)}.{nameof(UriEndPoint.Uri)} passed to {nameof(ConnectAsync)}.");
            }

            var shallowCopiedOptions = ShallowCopyHttpConnectionOptions(_options);
            shallowCopiedOptions.Url = uriEndPoint.Uri;

            var connection = new BlazorHttpConnection(
                shallowCopiedOptions, _jsRuntime, _navigationManager, _loggerFactory);

            try
            {
                await connection.StartAsync();
                return connection;
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        // Internal for testing
        internal static BlazorHttpConnectionOptions ShallowCopyHttpConnectionOptions(
            BlazorHttpConnectionOptions options)
        {
            return new BlazorHttpConnectionOptions
            {
                HttpMessageHandlerFactory = options.HttpMessageHandlerFactory,
                Headers = options.Headers,
                //ClientCertificates = options.ClientCertificates,
                //Cookies = options.Cookies,
                Url = options.Url,
                Transports = options.Transports,
                SkipNegotiation = options.SkipNegotiation,
                AccessTokenProvider = options.AccessTokenProvider,
                //CloseTimeout = options.CloseTimeout,
                //Credentials = options.Credentials,
                //Proxy = options.Proxy,
                //UseDefaultCredentials = options.UseDefaultCredentials,
                DefaultTransferFormat = options.DefaultTransferFormat,
                //WebSocketConfiguration = options.WebSocketConfiguration,
            };
        }
    }
}
