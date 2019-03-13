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
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Components;

namespace AI4E.AspNetCore.Components
{
    /// <summary>
    /// Resolves components for an application.
    /// </summary>
    public static class ComponentResolver
    {
        // TODO: Rename
        public static Assembly BlazorAssembly { get; } = typeof(IComponent).Assembly;

        /// <summary>
        /// Lists all the types 
        /// </summary>
        /// <param name="appAssembly"></param>
        /// <returns></returns>
        public static IEnumerable<Type> ResolveComponents(Assembly appAssembly)
        {
            return EnumerateComponentAssemblies(appAssembly).SelectMany(a => GetComponents(a));
        }

        public static IEnumerable<Type> GetComponents(Assembly assembly)
        {
            return assembly.ExportedTypes.Where(t => typeof(IComponent).IsAssignableFrom(t));
        }

        public static IEnumerable<Assembly> EnumerateComponentAssemblies(Assembly assembly)
        {
            return EnumerateComponentAssemblies(assembly, AssemblyLoadContext.Default);
        }

        public static IEnumerable<Assembly> EnumerateComponentAssemblies(Assembly assembly, AssemblyLoadContext loadContext)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            if (loadContext == null)
                throw new ArgumentNullException(nameof(loadContext));

            var assemblyName = assembly.GetName();
            var visited = new HashSet<Assembly>(new AssemblyComparer());
            return EnumerateAssemblies(assemblyName, loadContext, visited);
        }

        private static IEnumerable<Assembly> EnumerateAssemblies(
            AssemblyName assemblyName,
            AssemblyLoadContext loadContext,
            HashSet<Assembly> visited)
        {
            var assembly = loadContext.LoadFromAssemblyName(assemblyName);
            if (visited.Contains(assembly))
            {
                // Avoid traversing visited assemblies.
                yield break;
            }
            visited.Add(assembly);
            var references = assembly.GetReferencedAssemblies();
            if (!references.Any(r => string.Equals(r.FullName, BlazorAssembly.FullName, StringComparison.Ordinal)))
            {
                // Avoid traversing references that don't point to blazor (like netstandard2.0)
                yield break;
            }
            else
            {
                yield return assembly;

                // Look at the list of transitive dependencies for more components.
                foreach (var reference in references.SelectMany(r => EnumerateAssemblies(r, loadContext, visited)))
                {
                    yield return reference;
                }
            }
        }

        private class AssemblyComparer : IEqualityComparer<Assembly>
        {
            public bool Equals(Assembly x, Assembly y)
            {
                return string.Equals(x?.FullName, y?.FullName, StringComparison.Ordinal);
            }

            public int GetHashCode(Assembly obj)
            {
                return obj.FullName.GetHashCode();
            }
        }
    }
}
