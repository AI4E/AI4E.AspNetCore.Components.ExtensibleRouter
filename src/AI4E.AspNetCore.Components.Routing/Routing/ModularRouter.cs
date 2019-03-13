using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace AI4E.AspNetCore.Components.Routing
{
    public class ModularRouter : ExtensibleRouter
    {
        private bool _lastRoutingSuccessful = true;

        [Inject] private IAssemblySource AssemblySoure { get; set; }

        /// <inheritdoc />
        protected override IEnumerable<Type> ResolveRoutableComponents()
        {
            return AssemblySoure.Assemblies.SelectMany(p => ComponentResolver.GetComponents(p));
        }

        /// <inheritdoc />
        protected override void OnInit()
        {
            if (AssemblySoure != null)
            {
                AssemblySoure.AssembliesChanged += AssembliesChanged;
            }

            base.OnInit();
        }

        private void AssembliesChanged(object sender, EventArgs e)
        {
            UpdateRouteTable();

            if (!_lastRoutingSuccessful)
                Refresh();
        }

        protected override void OnAfterRefresh(bool success)
        {
            _lastRoutingSuccessful = success;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (AssemblySoure != null)
            {
                AssemblySoure.AssembliesChanged -= AssembliesChanged;
            }

            base.Dispose(disposing);
        }
    }

    public interface IAssemblySource
    {
        IReadOnlyCollection<Assembly> Assemblies { get; }
        event EventHandler AssembliesChanged;
    }
}
