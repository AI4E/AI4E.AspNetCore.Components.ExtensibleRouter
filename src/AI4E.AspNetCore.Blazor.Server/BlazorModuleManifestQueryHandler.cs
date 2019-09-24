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
using System.Threading;
using System.Threading.Tasks;

namespace AI4E.AspNetCore.Blazor.Server
{
#pragma warning disable CA1812
    [MessageHandler]
    internal sealed class BlazorModuleManifestQueryHandler
#pragma warning restore CA1812
    {
        private readonly IBlazorModuleManifestProvider _manifestProvider;

        public BlazorModuleManifestQueryHandler(IBlazorModuleManifestProvider manifestProvider)
        {
            if (manifestProvider == null)
                throw new ArgumentNullException(nameof(manifestProvider));

            _manifestProvider = manifestProvider;
        }


        public ValueTask<BlazorModuleManifest> HandleAsync(
#pragma warning disable IDE0060, CA1801
            Query<BlazorModuleManifest> query,
#pragma warning restore IDE0060, CA1801
            CancellationToken cancellation)

        {
            return _manifestProvider.GetBlazorModuleManifestAsync(cancellation);
        }
    }
}
