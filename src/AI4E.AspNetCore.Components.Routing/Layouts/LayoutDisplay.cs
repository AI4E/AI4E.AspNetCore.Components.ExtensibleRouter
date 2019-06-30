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
 * AspNet Core (https://github.com/aspnet/AspNetCore)
 * Copyright (c) .NET Foundation. All rights reserved.
 * Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
 * --------------------------------------------------------------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Layouts;
using Microsoft.AspNetCore.Components.RenderTree;

namespace AI4E.AspNetCore.Components.Layouts
{
    /// <summary>
    /// Displays the specified page component, rendering it inside its layout
    /// and any further nested layouts.
    /// </summary>
    public class LayoutDisplay : IComponent
    {
        internal const string NameOfPage = nameof(Page);
        internal const string NameOfPageParameters = nameof(PageParameters);

        private RenderHandle _renderHandle;

        /// <summary>
        /// Gets or sets the type of the page component to display.
        /// The type must implement <see cref="IComponent"/>.
        /// </summary>
        [Parameter]
        public Type Page { get; private set; }

        /// <summary>
        /// Gets or sets the parameters to pass to the page.
        /// </summary>
        [Parameter]
        public IDictionary<string, object> PageParameters { get; private set; }

        /// <inheritdoc />
        public void Configure(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        /// <inheritdoc />
        public Task SetParametersAsync(ParameterCollection parameters)
        {
            parameters.SetParameterProperties(this);
            Render();
            return Task.CompletedTask;
        }

        private void Render()
        {
            // In the middle, we render the requested page
            var fragment = RenderComponentWithBody(Page, bodyParam: null);

            // Repeatedly wrap it in each layer of nested layout until we get
            // to a layout that has no parent
            var layoutType = Page;
            while ((layoutType = GetLayoutType(layoutType)) != null)
            {
                fragment = RenderComponentWithBody(layoutType, fragment);
            }

            _renderHandle.Render(fragment);
        }

        private RenderFragment RenderComponentWithBody(Type componentType, RenderFragment bodyParam)
        {
            void RenderFragment(RenderTreeBuilder builder)
            {
                builder.OpenComponent(0, componentType);
                if (bodyParam != null)
                {
                    builder.AddAttribute(1, "Body", bodyParam);
                }
                else
                {
                    if (PageParameters != null)
                    {
                        foreach (var kvp in PageParameters)
                        {
                            builder.AddAttribute(1, kvp.Key, kvp.Value);
                        }
                    }
                }
                builder.CloseComponent();
            }

            return RenderFragment;
        }

        private Type GetLayoutType(Type type)
        {
            return type.GetCustomAttribute<LayoutAttribute>()?.LayoutType;
        }
    }
}
