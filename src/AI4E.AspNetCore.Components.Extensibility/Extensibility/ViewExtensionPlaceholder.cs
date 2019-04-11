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
    public sealed class ViewExtensionPlaceholder<TViewExtension> : IComponent, IDisposable
        where TViewExtension : IViewExtensionDefinition
    {
        private RenderHandle _renderHandle;
        private bool _isInit;
        private ParameterCollection _parameters;
        private HashSet<Type> _viewExtensions;

        [Inject] private IAssemblySource AssemblySource { get; set; }

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
            foreach (var parameter in _parameters)
            {
                throw new NotImplementedException(); // TODO
            }
        }
    }
}
