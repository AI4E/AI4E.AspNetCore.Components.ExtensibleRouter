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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AI4E.AspNetCore.Blazor.SignalR
{
    internal class LoggingHttpMessageHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingHttpMessageHandler>? _logger;

        public LoggingHttpMessageHandler(HttpMessageHandler inner, ILoggerFactory? loggerFactory)
            : base(inner)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory?.CreateLogger<LoggingHttpMessageHandler>();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Log.SendingHttpRequest(_logger, request.Method, request.RequestUri);
            var httpResponseMessage = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                Log.UnsuccessfulHttpResponse(_logger, httpResponseMessage.StatusCode, request.Method,
                                  request.RequestUri);
            }

            return httpResponseMessage;
        }

        private static class Log
        {
            private static readonly Action<ILogger, HttpMethod, Uri, Exception?> _sendingHttpRequest =
                LoggerMessage.Define<HttpMethod, Uri>(LogLevel.Trace, new EventId(1, "SendingHttpRequest"),
                    "Sending HTTP request {RequestMethod} '{RequestUrl}'.");

            private static readonly Action<ILogger, int, HttpMethod, Uri, Exception?> _unsuccessfulHttpResponse =
                LoggerMessage.Define<int, HttpMethod, Uri>(LogLevel.Warning, new EventId(2, "UnsuccessfulHttpResponse"),
                    "Unsuccessful HTTP response {StatusCode} return from {RequestMethod} '{RequestUrl}'.");

            public static void SendingHttpRequest(ILogger? logger, HttpMethod requestMethod, Uri requestUrl)
            {
                if (logger is null)
                    return;

                Log._sendingHttpRequest(logger, requestMethod, requestUrl, null);
            }

            public static void UnsuccessfulHttpResponse(ILogger? logger, HttpStatusCode statusCode,
                HttpMethod requestMethod, Uri requestUrl)
            {
                if (logger is null)
                    return;

                Log._unsuccessfulHttpResponse(logger, (int)statusCode, requestMethod, requestUrl, null);
            }
        }
    }
}
