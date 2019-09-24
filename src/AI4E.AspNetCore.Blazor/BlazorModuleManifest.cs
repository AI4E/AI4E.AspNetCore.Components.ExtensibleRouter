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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AI4E.AspNetCore.Blazor
{
#if BLAZOR
    internal
#else
    public
#endif
#pragma warning disable CA1812
        sealed class BlazorModuleManifest
#pragma warning restore CA1812
    {
        public string Name { get; set; } = null!;

#pragma warning disable CA2227
        public List<BlazorModuleManifestAssemblyEntry> Assemblies { get; set; } = new List<BlazorModuleManifestAssemblyEntry>();
#pragma warning restore CA2227
    }

#if BLAZOR
    internal
#else
    public
#endif
#pragma warning disable CA1812
        sealed class BlazorModuleManifestAssemblyEntry
#pragma warning restore CA1812
    {
        public string AssemblyName { get; set; } = null!;

        [JsonConverter(typeof(VersionConverter))]
        public Version AssemblyVersion { get; set; } = null!;
        public bool IsComponentAssembly { get; set; }
    }
}
