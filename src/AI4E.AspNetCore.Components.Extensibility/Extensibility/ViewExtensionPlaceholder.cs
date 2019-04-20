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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;

namespace AI4E.AspNetCore.Components.Extensibility
{
    /// <summary>
    /// A placeholder for view extensions, that renders all available view extensions.
    /// </summary>
    /// <typeparam name="TViewExtension">The type of view extension definition.</typeparam>
    public sealed class ViewExtensionPlaceholder<TViewExtension> : IComponent, IDisposable
        where TViewExtension : IViewExtensionDefinition
    {
        internal const string ContextName = nameof(Context);

        private RenderHandle _renderHandle;
        private bool _isInit;
        private ParameterCollection _parameters;
        private HashSet<Type> _viewExtensions;

        [Inject] private IAssemblySource AssemblySource { get; set; }
        [Parameter] private object Context { get; set; }

        /// <inheritdoc />
        public void Configure(RenderHandle renderHandle)
        {
            if (_renderHandle.IsInitialized)
            {
                throw new InvalidOperationException("Cannot set the render handler to the component multiple times.");
            }

            _renderHandle = renderHandle;
        }

        /// <inheritdoc />
        public Task SetParametersAsync(ParameterCollection parameters)
        {
            _parameters = parameters;
            Context = _parameters.GetValueOrDefault<object>(ContextName);

            if (!_isInit)
            {
                _isInit = true;

                Init();
            }

            UpdateViewExtensions();
            Render();
            return Task.CompletedTask;
        }

        private void Init()
        {
            AssemblySource.AssembliesChanged += AssembliesChanged;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            AssemblySource.AssembliesChanged -= AssembliesChanged;
        }

        private void AssembliesChanged(object sender, EventArgs e)
        {
            if (UpdateViewExtensions())
            {
                Render();
            }
        }

        private bool UpdateViewExtensions()
        {
            var assemblies = AssemblySource.Assemblies;
            var viewExtensions = assemblies.SelectMany(a => GetViewExtensions(a));

            if (_viewExtensions == null)
            {
                _viewExtensions = new HashSet<Type>(viewExtensions);
                return true;
            }

            if (!_viewExtensions.SetEquals(viewExtensions))
            {
                _viewExtensions.Clear();
                _viewExtensions.UnionWith(viewExtensions);
                return true;
            }

            return false;
        }

        // TODO: The result for each assembly can be cached.
        private static IEnumerable<Type> GetViewExtensions(Assembly assembly)
        {
            return assembly.ExportedTypes.Where(t => typeof(TViewExtension).IsAssignableFrom(t) && !t.IsInterface);
        }

        private void Render()
        {
            RenderFragment renderFragment = Render;
            _renderHandle.Render(renderFragment);
        }

        private void Render(RenderTreeBuilder builder)
        {
            Debug.Assert(_viewExtensions != null);
            foreach (var viewExtension in _viewExtensions)
            {
                builder.OpenComponent(0, viewExtension);

                ApplyParameters(builder);

                builder.CloseComponent();
            }
        }

        private void ApplyParameters(RenderTreeBuilder builder)
        {
            if (Context != null)
            {
                builder.AddAttribute(0, ContextName, Context);
            }
        }
    }
}
