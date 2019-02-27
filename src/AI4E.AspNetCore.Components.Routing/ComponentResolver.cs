/* License
 * --------------------------------------------------------------------------------------------------------------------
 * This file is part of the AI4E distribution.
 *   (https://github.com/AI4E/AI4E.AspNetCore.Components.ExtensibleRouter)
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
using Microsoft.AspNetCore.Components;

namespace AI4E.AspNetCore.Components
{
    /// <summary>
    /// Resolves components for an application.
    /// </summary>
    public static class ComponentResolver
    {
        /// <summary>
        /// Lists all the types 
        /// </summary>
        /// <param name="appAssembly"></param>
        /// <returns></returns>
        public static IEnumerable<Type> ResolveComponents(Assembly appAssembly)
        {
            var blazorAssembly = typeof(IComponent).Assembly;

            return EnumerateAssemblies(appAssembly.GetName(), blazorAssembly, new HashSet<Assembly>(new AssemblyComparer()))
                .SelectMany(a => a.ExportedTypes)
                .Where(t => typeof(IComponent).IsAssignableFrom(t));
        }

        private static IEnumerable<Assembly> EnumerateAssemblies(
            AssemblyName assemblyName,
            Assembly blazorAssembly,
            HashSet<Assembly> visited)
        {
            var assembly = Assembly.Load(assemblyName);
            if (visited.Contains(assembly))
            {
                // Avoid traversing visited assemblies.
                yield break;
            }
            visited.Add(assembly);
            var references = assembly.GetReferencedAssemblies();
            if (!references.Any(r => string.Equals(r.FullName, blazorAssembly.FullName, StringComparison.Ordinal)))
            {
                // Avoid traversing references that don't point to blazor (like netstandard2.0)
                yield break;
            }
            else
            {
                yield return assembly;

                // Look at the list of transitive dependencies for more components.
                foreach (var reference in references.SelectMany(r => EnumerateAssemblies(r, blazorAssembly, visited)))
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
