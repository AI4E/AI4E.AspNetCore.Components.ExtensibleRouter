/* License
 * --------------------------------------------------------------------------------------------------------------------
 * This file is part of the AI4E distribution.
 *   (https://github.com/AI4E/AI4E.AspNetCore.Components.ExtensibleRouter)
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
 * AspNet Core (https://github.com/aspnet/AspNetCore)
 * Copyright (c) .NET Foundation. All rights reserved.
 * Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
 * --------------------------------------------------------------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Layouts;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Services;

namespace AI4E.AspNetCore.Components.Routing
{
    /// <summary>
    /// A base type for custom component routers.
    /// </summary>
    public abstract class ExtensibleRouter : IComponent, IDisposable
    {
        private const string _layoutNameOfPage = "Page";
        private const string _layoutNameOfPageParameters = "PageParameters";


        private static readonly char[] _queryOrHashStartChar = new[] { '?', '#' };
        private RenderHandle _renderHandle;
        private string _baseUri;
        private string _locationAbsolute;

        [Inject] private IUriHelper UriHelper { get; set; }

        /// <summary>
        /// Gets or sets the type of the component that should be used as a fallback when no match is found for the requested route.
        /// </summary>
        [Parameter] private Type FallbackComponent { get; set; }

        private RouteTable Routes { get; set; }

        /// <inheritdoc />
        public void Configure(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
            _baseUri = UriHelper.GetBaseUri();
            _locationAbsolute = UriHelper.GetAbsoluteUri();
            UriHelper.OnLocationChanged += OnLocationChanged;
        }

        /// <inheritdoc />
        public Task SetParametersAsync(ParameterCollection parameters)
        {
            parameters.SetParameterProperties(this);
            var types = ResolveRoutableComponents();
            Routes = RouteTable.Create(types);
            Refresh();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Resolves the types of components that can be routed to.
        /// </summary>
        /// <returns>An enumerable of types of components that can be routed to.</returns>
        protected abstract IEnumerable<Type> ResolveRoutableComponents();

        protected virtual void NoRouteMatching(RenderHandle renderHandle, string location)
        {
            throw new InvalidOperationException($"'{nameof(DefaultRouter)}' cannot find any component with a route for '/{location}', and no fallback is defined.");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Frees resources used by the component.
        /// </summary>
        /// <param name="disposing">A boolean value indicating whether this is a managed dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            UriHelper.OnLocationChanged -= OnLocationChanged;
        }

        private string StringUntilAny(string str, char[] chars)
        {
            var firstIndex = str.IndexOfAny(chars);
            return firstIndex < 0
                ? str
                : str.Substring(0, firstIndex);
        }

        /// <inheritdoc />
        protected virtual void Render(RenderTreeBuilder builder, Type handler, IDictionary<string, object> parameters)
        {
            builder.OpenComponent(0, typeof(LayoutDisplay));
            builder.AddAttribute(1, _layoutNameOfPage, handler);
            builder.AddAttribute(2, _layoutNameOfPageParameters, parameters);
            builder.CloseComponent();
        }

        private void Refresh()
        {
            var locationPath = UriHelper.ToBaseRelativePath(_baseUri, _locationAbsolute);
            locationPath = StringUntilAny(locationPath, _queryOrHashStartChar);
            var context = new RouteContext(locationPath);
            Routes.Route(context);

            if (context.Handler == null)
            {
                if (FallbackComponent != null)
                {
                    context.Handler = FallbackComponent;
                }
                else
                {
                    NoRouteMatching(_renderHandle, locationPath);
                    return;
                }
            }

            if (!typeof(IComponent).IsAssignableFrom(context.Handler))
            {
                throw new InvalidOperationException($"The type {context.Handler.FullName} " +
                    $"does not implement {typeof(IComponent).FullName}.");
            }

            _renderHandle.Render(builder => Render(builder, context.Handler, context.Parameters));
        }

        private void OnLocationChanged(object sender, string newAbsoluteUri)
        {
            _locationAbsolute = newAbsoluteUri;
            if (_renderHandle.IsInitialized)
            {
                Refresh();
            }
        }
    }
}
