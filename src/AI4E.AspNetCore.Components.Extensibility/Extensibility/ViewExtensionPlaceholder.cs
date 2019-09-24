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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace AI4E.AspNetCore.Components.Extensibility
{
    /// <summary>
    /// A placeholder for view extensions, that renders all available view extensions.
    /// </summary>
    /// <typeparam name="TViewExtension">The type of view extension definition.</typeparam>
    public sealed class ViewExtensionPlaceholder<TViewExtension> : IComponent, IDisposable
        where TViewExtension : IViewExtensionDefinition
    {
        private readonly RenderFragment _renderFragment;  // Cache to avoid per-render allocations

        private RenderHandle _renderHandle;
        private bool _isInit;
        private HashSet<Type>? _viewExtensions;

        /// <summary>
        /// Creates a new instance of the <see cref="ViewExtensionPlaceholder{TViewExtension}"/> type.
        /// </summary>
        public ViewExtensionPlaceholder()
        {
            _renderFragment = Render;
        }

        [Inject] private IAssemblySource AssemblySource { get; set; } = null!;

        /// <summary>
        /// Gets or sets the view-extension context.
        /// </summary>
        [Parameter] public object? Context { get; set; }

        /// <summary>
        /// Gets or sets a collection of attributes that will be applied to the rendered view-extension.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object>? ViewExtensionAttributes { get; set; }

        /// <inheritdoc />
        public void Attach(RenderHandle renderHandle)
        {
            if (_renderHandle.IsInitialized)
            {
                throw new InvalidOperationException("Cannot set the render handler to the component multiple times.");
            }

            _renderHandle = renderHandle;
        }

        /// <inheritdoc />
        public Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);

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

        private void AssembliesChanged(object? sender, EventArgs e)
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

        private static readonly ConcurrentDictionary<Assembly, ImmutableList<Type>> ViewExtensionsLookup
            = new ConcurrentDictionary<Assembly, ImmutableList<Type>>();

        private static ImmutableList<Type> GetViewExtensions(Assembly assembly)
        {
            if (ViewExtensionsLookup.TryGetValue(assembly, out var result))
            {
                return result;
            }

            result = assembly.ExportedTypes.Where(IsViewExtension).ToImmutableList();
            ViewExtensionsLookup.TryAdd(assembly, result);

            return result;
        }

        private static bool IsViewExtension(Type type)
        {
            if (!typeof(TViewExtension).IsAssignableFrom(type))
                return false;

            if (type.IsInterface)
                return false;

            // The view-extension definition itself is not a view-extension we may consider,
            // otherwise we end up in an infinite loop.
            return type != typeof(TViewExtension);
        }

        private void Render()
        {
            _renderHandle.Render(_renderFragment);
        }

        private void Render(RenderTreeBuilder builder)
        {
            Debug.Assert(_viewExtensions != null);
            foreach (var viewExtension in _viewExtensions!)
            {
                builder.OpenComponent(sequence: 0, viewExtension);
                ApplyParameters(builder);
                builder.CloseComponent();
            }
        }

        private void ApplyParameters(RenderTreeBuilder builder)
        {
            if (Context != null)
            {
                builder.AddAttribute(0, nameof(Context), Context);
            }

            if (ViewExtensionAttributes != null)
            {
                builder.AddMultipleAttributes(sequence: 0, ViewExtensionAttributes);
            }
        }
    }
}
