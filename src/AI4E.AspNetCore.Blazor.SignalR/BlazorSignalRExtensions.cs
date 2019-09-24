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
using AI4E.AspNetCore.Blazor.SignalR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public static class BlazorSignalRExtensions
    {
        public static IHubConnectionBuilder WithUrlBlazor(
            this IHubConnectionBuilder hubConnectionBuilder,
            string url,
            IJSRuntime jsRuntime,
            NavigationManager navigationManager,
            HttpTransportType? transports = null,
            Action<BlazorHttpConnectionOptions>? options = null)
        {
            return WithUrlBlazor(hubConnectionBuilder, new Uri(url), jsRuntime, navigationManager, transports, options);
        }

        public static IHubConnectionBuilder WithUrlBlazor(
            this IHubConnectionBuilder hubConnectionBuilder,
            Uri url,
            IJSRuntime jsRuntime,
            NavigationManager navigationManager,
            HttpTransportType? transports = null,
            Action<BlazorHttpConnectionOptions>? options = null)
        {
            if (hubConnectionBuilder == null)
                throw new ArgumentNullException(nameof(hubConnectionBuilder));

            if (jsRuntime == null)
                throw new ArgumentNullException(nameof(jsRuntime));

            if (navigationManager is null)
                throw new ArgumentNullException(nameof(navigationManager));

            hubConnectionBuilder.Services.Configure<BlazorHttpConnectionOptions>(o =>
            {
                o.Url = url;

                if (!transports.HasValue)
                    return;

                o.Transports = transports.Value;
            });

            if (options != null)
                hubConnectionBuilder.Services.Configure(options);

            hubConnectionBuilder.Services.AddSingleton<EndPoint, BlazorHttpConnectionOptionsDerivedHttpEndPoint>();

            hubConnectionBuilder.Services.AddSingleton<
                IConfigureOptions<BlazorHttpConnectionOptions>, BlazorHubProtocolDerivedHttpOptionsConfigurer>();

            hubConnectionBuilder.Services.AddSingleton(
                provider => BuildBlazorHttpConnectionFactory(provider, jsRuntime, navigationManager));

            return hubConnectionBuilder;
        }

#pragma warning disable CA1812
        private class BlazorHttpConnectionOptionsDerivedHttpEndPoint : UriEndPoint
#pragma warning restore CA1812
        {
            public BlazorHttpConnectionOptionsDerivedHttpEndPoint(IOptions<BlazorHttpConnectionOptions> options)
                : base(options.Value.Url)
            { }
        }
#pragma warning disable CA1812
        private class BlazorHubProtocolDerivedHttpOptionsConfigurer
            : IConfigureNamedOptions<BlazorHttpConnectionOptions>
#pragma warning restore CA1812
        {
            private readonly TransferFormat _defaultTransferFormat;

            public BlazorHubProtocolDerivedHttpOptionsConfigurer(IHubProtocol hubProtocol)
            {
                _defaultTransferFormat = hubProtocol.TransferFormat;
            }

            public void Configure(string name, BlazorHttpConnectionOptions options)
            {
                Configure(options);
            }

            public void Configure(BlazorHttpConnectionOptions options)
            {
                options.DefaultTransferFormat = _defaultTransferFormat;
            }
        }

        private static IConnectionFactory BuildBlazorHttpConnectionFactory(
            IServiceProvider provider,
            IJSRuntime jsRuntime,
            NavigationManager navigationManager)
        {
            return ActivatorUtilities.CreateInstance<BlazorHttpConnectionFactory>(
                provider,
                jsRuntime,
                navigationManager);
        }
    }
}
