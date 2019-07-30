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
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;

namespace AI4E.AspNetCore.Blazor.SignalR
{
    public class BlazorHttpConnectionOptions
    {
        private IDictionary<string, string> _headers;

        public BlazorHttpConnectionOptions()
        {
            _headers = new Dictionary<string, string>();
            Transports = HttpTransports.All;
            Implementations = BlazorTransportType.JsWebSockets | BlazorTransportType.ManagedWebSockets |
                              BlazorTransportType.JsServerSentEvents | BlazorTransportType.ManagedServerSentEvents |
                              BlazorTransportType.ManagedLongPolling;
        }

        /// <summary>
        /// Gets or sets a delegate for wrapping or replacing the
        /// <see cref="P:Microsoft.AspNetCore.Http.Connections.Client.HttpConnectionOptions.HttpMessageHandlerFactory" />
        /// that will make HTTP requests.
        /// </summary>
        public Func<HttpMessageHandler, HttpMessageHandler> HttpMessageHandlerFactory { get; set; }

        /// <summary>
        /// Gets or sets a collection of headers that will be sent with HTTP requests.
        /// </summary>
        public IDictionary<string, string> Headers
        {
            get => _headers;
            set
            {
                var dictionary = value;
                _headers = dictionary ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>Gets or sets the URL used to send HTTP requests.</summary>
        public Uri Url { get; set; }

        /// <summary>
        /// Gets or sets a bitmask comprised of one or more <see cref="T:Microsoft.AspNetCore.Http.Connections.HttpTransportType" />
        /// that specify what transports the client should use to send HTTP requests.
        /// </summary>
        public HttpTransportType Transports { get; set; }

        /// <summary>
        /// Gets or sets a bitmask comprised of one or more <see cref="T:BlazorSignalR.BlazorTransportType" />
        /// that specify what transports the client should use to send HTTP requests. Used to select what implementations to use.
        /// </summary>
        public BlazorTransportType Implementations { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether negotiation is skipped when connecting to the server.
        /// </summary>
        /// <remarks>
        /// Negotiation can only be skipped when using the <see cref="F:Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets" /> transport.
        /// </remarks>
        public bool SkipNegotiation { get; set; }

        /// <summary>
        /// Gets or sets an access token provider that will be called to return a token for each HTTP request.
        /// </summary>
        public Func<Task<string>> AccessTokenProvider { get; set; }

        /// <summary>
        /// Gets or sets the default <see cref="TransferFormat" /> to use if <see cref="HttpConnection.StartAsync(CancellationToken)"/>
        /// is called instead of <see cref="HttpConnection.StartAsync(TransferFormat, CancellationToken)"/>.
        /// </summary>
        public TransferFormat DefaultTransferFormat { get; set; } = TransferFormat.Binary;
    }
}
