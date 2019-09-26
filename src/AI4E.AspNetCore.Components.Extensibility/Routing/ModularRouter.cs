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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AI4E.AspNetCore.Components.Extensibility;
using Microsoft.AspNetCore.Components;

namespace AI4E.AspNetCore.Components.Routing
{
    /// <summary>
    /// A router that loads the component assemblies from a <see cref="IAssemblySource"/>.
    /// </summary>
    public class ModularRouter : ExtensibleRouter
    {
        protected internal RouteData? PreviousRouteData { get; private set; }

        [Inject] private IAssemblySource AssemblySource { get; set; } = null!;

        /// <inheritdoc />
        protected override IEnumerable<Type> ResolveRoutableComponents()
        {
            return AssemblySource.Assemblies.SelectMany(p => ComponentResolver.GetComponents(p));
        }

        /// <inheritdoc />
        protected override void OnInit()
        {
            if (AssemblySource != null)
            {
                AssemblySource.AssembliesChanged += AssembliesChanged;
            }

            base.OnInit();
        }

        private ValueTask AssembliesChanged(
            IAssemblySource sender,
            IReadOnlyCollection<Assembly> assemblies)
        {
            return InvokeAsync(() =>
            {
                UpdateRouteTable();

                // Check whether we have to refresh. This is the case if any of:
                // - The last routing was not successful
                // - The current route handler is of an assembly that is unavailable (is currently in an unload process) assembly
                // - The previous route handle is of an assembly that is unavailable assembly (the components and types are still stored in the render tree for diff building)
                if (NeedsRefresh(out var routeIsOfUnloadedAssembly))
                {
                    Refresh();
                }

                // We need to refresh again, if the route handle before the refresh above is of an assembly that is unavailable assembly.
                // With the above refresh the components and types are still stored in the render tree for diff building and we have to refresh again to remove them.
                if (routeIsOfUnloadedAssembly)
                {
                    Refresh();
                }

            }).AsValueTask();
        }

        private bool NeedsRefresh(out bool routeIsOfUnloadedAssembly)
        {
            routeIsOfUnloadedAssembly = false;

            if (RouteData is null)
            {
                return true;
            }

            if (RouteIsOfUnloadedAssembly())
            {
                routeIsOfUnloadedAssembly = true;
                return true;
            }

            return PreviousRouteIsOfUnloadedAssembly();
        }

        private bool RouteIsOfUnloadedAssembly()
        {
            Debug.Assert(RouteData != null);
            var pageType = RouteData!.PageType;
            var pageTypeAssembly = pageType.Assembly;

            return !AssemblySource.ContainsAssembly(pageTypeAssembly);
        }

        private bool PreviousRouteIsOfUnloadedAssembly()
        {
            if (PreviousRouteData is null)
            {
                return false;
            }

            var previousPageType = PreviousRouteData.PageType;
            var previousPageTypeAssembly = previousPageType.Assembly;

            return !AssemblySource.ContainsAssembly(previousPageTypeAssembly);
        }

        protected override void OnAfterRefresh(bool success)
        {
            PreviousRouteData = RouteData;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (AssemblySource != null)
            {
                AssemblySource.AssembliesChanged -= AssembliesChanged;
            }

            base.Dispose(disposing);
        }
    }
}
