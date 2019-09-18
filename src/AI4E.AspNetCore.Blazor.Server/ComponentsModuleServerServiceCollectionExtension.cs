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
using AI4E.AspNetCore.Blazor.Server;
using AI4E.Modularity.Metadata;
using AI4E.Utils.ApplicationParts;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ComponentsModuleServerServiceCollectionExtension
    {
        public static void AddBlazorModuleServer(this IServiceCollection services, Assembly appAssembly)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.ConfigureApplicationParts(ConfigureApplicationParts);
            services.AddSingleton<IBlazorModuleManifestProvider, BlazorModuleManifestProvider>(
                p => new BlazorModuleManifestProvider(appAssembly, p.GetRequiredService<IMetadataAccessor>()));
        }

        private static void ConfigureApplicationParts(ApplicationPartManager partManager)
        {
            partManager.ApplicationParts.Add(new AssemblyPart(Assembly.GetExecutingAssembly()));
        }
    }
}
