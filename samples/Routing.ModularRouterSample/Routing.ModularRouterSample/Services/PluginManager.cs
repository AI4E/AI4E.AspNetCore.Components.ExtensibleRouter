using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using AI4E.AspNetCore.Components.Extensibility;

namespace Routing.ModularRouterSample.Services
{
    public class PluginManager
    {
        private readonly AssemblyManager _assemblyManager;
        private readonly Dictionary<AssemblyName, Assembly> _hostAssemblies;
        private PluginAssemblyLoadContext _loadContext;
        private Assembly _pluginAssembly;

        public PluginManager(AssemblyManager assemblyManager)
        {
            _assemblyManager = assemblyManager;
            _hostAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToDictionary(p => p.GetName(), p => p, new AssemblyNameComparer());
        }

        private const string _pluginName = "Routing.ModularRouterSample.Plugin";

        private static string GetPluginPath()
        {
            var dir = Assembly.GetExecutingAssembly().Location;
            var targetFramework = Path.GetFileName(dir = GetDirectoryName(dir));
            var configuration = Path.GetFileName(dir = GetDirectoryName(dir));
            dir = GetDirectoryName(dir);
            dir = GetDirectoryName(dir);

            var pluginAssemblyDir = Path.Combine(dir, _pluginName, configuration, "netstandard2.0");
            return Path.Combine(pluginAssemblyDir, _pluginName + ".dll");
        }

        private static string GetDirectoryName(string path)
        {
            path = Path.GetDirectoryName(path);
            if (path.Last() == '/' || path.Last() == '\\')
            {
                path = path.Substring(0, path.Length - 1);
            }

            return path;
        }

        public void InstallPlugin()
        {
            if (IsPluginInstalled)
                return;

            var assemblyPath = GetPluginPath();
            _loadContext = new PluginAssemblyLoadContext(assemblyPath, _hostAssemblies);
            _pluginAssembly = _loadContext.LoadFromAssemblyPath(assemblyPath);

            _assemblyManager.AddAssembly(_pluginAssembly);
            IsPluginInstalled = true;
        }

        public void UninstallPlugin()
        {
            if (!IsPluginInstalled)
                return;

            Unload(out var weakRef);
            for (var i = 0; weakRef.IsAlive && (i < 10); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            if (weakRef.IsAlive)
            {
                throw new Exception("Unable to unload plugin.");
            }

            IsPluginInstalled = false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Unload(out WeakReference weakRef)
        {
            _assemblyManager.RemoveAssembly(_pluginAssembly);

            _loadContext.Unload();
            weakRef = new WeakReference(_loadContext);
            _loadContext = null;
            _pluginAssembly = null;
        }

        public bool IsPluginInstalled { get; private set; }

        // https://github.com/dotnet/samples/blob/master/core/tutorials/Unloading/Host/Program.cs
        private sealed class PluginAssemblyLoadContext : AssemblyLoadContext
        {
            private readonly AssemblyDependencyResolver _resolver;
            private readonly Dictionary<AssemblyName, Assembly> _hostAssemblies;

            public PluginAssemblyLoadContext(string pluginPath, Dictionary<AssemblyName, Assembly> hostAssemblies) : base(isCollectible: true)
            {
                _resolver = new AssemblyDependencyResolver(pluginPath);
                _hostAssemblies = hostAssemblies;
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                if (_hostAssemblies.TryGetValue(assemblyName, out var assembly))
                {
                    return assembly;
                }

                var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
                if (assemblyPath != null)
                {
                    Console.WriteLine($"Loading assembly {assemblyPath} into the HostAssemblyLoadContext");
                    return LoadFromAssemblyPath(assemblyPath);
                }

                return null;
            }
        }

        private class AssemblyNameComparer : IEqualityComparer<AssemblyName>
        {
            public bool Equals(AssemblyName x, AssemblyName y)
            {
                return string.Equals(x?.FullName, y?.FullName, StringComparison.Ordinal);
            }

            public int GetHashCode(AssemblyName obj)
            {
                return obj.FullName.GetHashCode();
            }
        }
    }
}
