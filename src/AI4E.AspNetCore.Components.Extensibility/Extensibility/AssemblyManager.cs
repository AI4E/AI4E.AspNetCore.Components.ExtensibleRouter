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
using System.Reflection;

namespace AI4E.AspNetCore.Components.Extensibility
{
    /// <summary>
    /// Manages a set of assemblies.
    /// </summary>
    public class AssemblyManager : IAssemblySource
    {
        private readonly HashSet<Assembly> _assemblies = new HashSet<Assembly>();

        /// <summary>
        /// Creates a new instance of the <see cref="AssemblyManager"/> type.
        /// </summary>
        public AssemblyManager() { }

        /// <summary>
        /// Creates a new instance of the <see cref="AssemblyManager"/> type that
        /// initially contains all assemblies that are dependencies of the specified
        /// assembly and contain components.
        /// </summary>
        /// <param name="entryAssembly">The entry assembly.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="entryAssembly"/> is <c>null</c>.</exception>
        public AssemblyManager(Assembly entryAssembly)
        {
            if (entryAssembly == null)
                throw new ArgumentNullException(nameof(entryAssembly));

            var assemblies = ComponentResolver.EnumerateComponentAssemblies(entryAssembly);
            foreach (var assembly in assemblies)
            {
                _assemblies.Add(assembly);
            }
        }

        /// <inheritdoc />
        public IReadOnlyCollection<Assembly> Assemblies => _assemblies;

        /// <inheritdoc />
        public event EventHandler AssembliesChanged;

        /// <summary>
        /// Adds an assembly to the manager.
        /// </summary>
        /// <param name="assembly">The assembly to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="assembly"/> is <c>null</c>.</exception>
        public void AddAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            if (_assemblies.Add(assembly))
            {
                NotifyAssembliesChanged();
            }
        }

        /// <summary>
        /// Removes an assembly from the manager.
        /// </summary>
        /// <param name="assembly">The assembly to remove.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="assembly"/> is <c>null</c>.</exception>
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
