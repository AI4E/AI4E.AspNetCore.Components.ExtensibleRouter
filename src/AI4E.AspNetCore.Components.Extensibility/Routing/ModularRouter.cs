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
using System.Linq;
using AI4E.AspNetCore.Components.Extensibility;
using Microsoft.AspNetCore.Components;

namespace AI4E.AspNetCore.Components.Routing
{
    /// <summary>
    /// A router that loads the component assemblies from a <see cref="IAssemblySource"/>.
    /// </summary>
    public class ModularRouter : ExtensibleRouter
    {
        private bool _lastRoutingSuccessful = true;

        [Inject] private IAssemblySource AssemblySoure { get; set; } = null!;

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

        private void AssembliesChanged(object? sender, EventArgs e)
        {
            UpdateRouteTable();

            if (!_lastRoutingSuccessful)
                Refresh();
        }

        /// <inheritdoc />
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
}
