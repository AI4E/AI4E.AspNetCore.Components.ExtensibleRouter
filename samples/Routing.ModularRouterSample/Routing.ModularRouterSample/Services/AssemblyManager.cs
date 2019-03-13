using System;
using System.Collections.Generic;
using System.Reflection;
using AI4E.AspNetCore.Components;
using AI4E.AspNetCore.Components.Routing;

namespace Routing.ModularRouterSample.Services
{
    public class AssemblyManager : IAssemblySource
    {
        private readonly HashSet<Assembly> _assemblies = new HashSet<Assembly>();

        public AssemblyManager()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            foreach (var assembly in ComponentResolver.EnumerateComponentAssemblies(entryAssembly))
            {
                _assemblies.Add(assembly);
            }
        }

        public IReadOnlyCollection<Assembly> Assemblies => _assemblies;

        public event EventHandler AssembliesChanged;

        public void AddAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            if (_assemblies.Add(assembly))
            {
                NotifyAssembliesChanged();
            }
        }

        public void RemoveAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            if (_assemblies.Remove(assembly))
            {
                NotifyAssembliesChanged();
            }
        }

        private void NotifyAssembliesChanged()
        {
            AssembliesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
