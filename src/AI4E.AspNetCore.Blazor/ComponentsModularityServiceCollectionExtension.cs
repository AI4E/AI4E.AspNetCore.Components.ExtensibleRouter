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
using System.Reflection;
using AI4E;
using AI4E.AspNetCore.Blazor;
using AI4E.AspNetCore.Components.Extensibility;
using AI4E.Modularity;
using AI4E.Routing.SignalR.Client;
using AI4E.Utils.ApplicationParts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ComponentsModularityServiceCollectionExtension
    {
        internal static readonly string _defaultHubUrl = "/MessageDispatcherHub"; // TODO: This should be configured only once.

        public static void AddBlazorMessageDispatcher(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            static void ConfigureHubConnection(
                IHubConnectionBuilder hubConnectionBuilder,
                IServiceProvider serviceProvider)
            {
                var jsRuntime = serviceProvider.GetRequiredService<IJSRuntime>();
                var navigationManager = serviceProvider.GetRequiredService<NavigationManager>();
                hubConnectionBuilder.WithUrlBlazor(new Uri(_defaultHubUrl, UriKind.Relative), jsRuntime, navigationManager);
            }

            services.AddSignalRMessageDispatcher(ConfigureHubConnection);
        }

        public static void AddBlazorModularity(this IServiceCollection services, Assembly entryAssembly)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton(new AssemblyManager(entryAssembly));
            services.AddSingleton<IAssemblySource>(p => p.GetRequiredService<AssemblyManager>());

            services.AddBlazorMessageDispatcher();

            services.AddSingleton<IRunningModuleManager, RemoteRunningModuleManager>();
            services.AddSingleton<IModulePropertiesLookup, RemoteModulePropertiesLookup>();
            services.AddSingleton<IModuleManifestProvider, ModuleManifestProvider>();
            services.AddSingleton<IModuleAssemblyDownloader, ModuleAssemblyDownloader>();
            services.AddSingleton<IInstallationSetManager, InstallationSetManager>();

            services.ConfigureApplicationParts(partManager => ConfigureApplicationParts(partManager, entryAssembly));
            services.ConfigureApplicationServices(ConfigureApplicationServices);

            services.AddSingleton(ServerSideIndicator.Instance);
        }

        public static void AddBlazorModularity(this IServiceCollection services)
        {
            AddBlazorModularity(services, Assembly.GetCallingAssembly());
        }

        private static void ConfigureApplicationServices(ApplicationServiceManager serviceManager)
        {
            serviceManager.AddService<IMessageDispatcher>();
            serviceManager.AddService<IInstallationSetManager>();
        }

        private static void ConfigureApplicationParts(ApplicationPartManager partManager, Assembly entryAssembly)
        {
            partManager.ApplicationParts.Add(new AssemblyPart(Assembly.GetExecutingAssembly()));
            partManager.ApplicationParts.Add(new AssemblyPart(entryAssembly));
        }
    }
}
