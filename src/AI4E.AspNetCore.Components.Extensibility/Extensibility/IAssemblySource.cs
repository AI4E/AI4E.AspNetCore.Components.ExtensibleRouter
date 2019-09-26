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
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace AI4E.AspNetCore.Components.Extensibility
{
    /// <summary>
    /// Represents a lookup for assembly that components can be load from.
    /// </summary>
    public interface IAssemblySource
    {
        /// <summary>
        /// Gets all known assemblies that contain components.
        /// </summary>
        IReadOnlyCollection<Assembly> Assemblies { get; }

        /// <summary>
        /// Notifies that the <see cref="Assemblies"/> collection changed.
        /// </summary>
        event AssembliedChangedEventHandler? AssembliesChanged;

        /// <summary>
        /// Returns a boolean value indicating whether the specified assembly may unload.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> to check.</param>
        /// <returns>A boolean value indicating whether <paramref name="assembly"/> is may unload.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="assembly"/> is <c>null</c>.</exception>
        bool CanUnload(Assembly assembly);

        /// <summary>
        /// Returns the <see cref="AssemblyLoadContext"/> that the specified assembly was loaded from.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/>.</param>
        /// <returns>The <see cref="AssemblyLoadContext"/> that <paramref name="assembly"/> was loaded from,
        /// or <c>null</c> if the <see cref="AssemblyLoadContext"/> of <paramref name="assembly"/> is not available.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="assembly"/> is <c>null</c>.</exception>
        AssemblyLoadContext? GetAssemblyLoadContext(Assembly assembly);

        /// <summary>
        /// Returns a boolean value indicating whether the specified assembly is contained in the assembly source.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/>.</param>
        /// <returns>A boolean value indicating whether <paramref name="assembly"/> is contained in the current assembly source.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="assembly"/> is <c>null</c>.</exception>
        bool ContainsAssembly(Assembly assembly);

        public delegate ValueTask AssembliedChangedEventHandler(
            IAssemblySource sender,
            IReadOnlyCollection<Assembly> assemblies);
    }
}
