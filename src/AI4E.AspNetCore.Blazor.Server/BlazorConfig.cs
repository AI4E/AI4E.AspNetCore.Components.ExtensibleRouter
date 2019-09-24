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
 * Asp.Net Blazor
 * Copyright (c) .NET Foundation. All rights reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use
 * these files except in compliance with the License. You may obtain a copy of the
 * License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software distributed
 * under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
 * CONDITIONS OF ANY KIND, either express or implied. See the License for the
 * specific language governing permissions and limitations under the License.
 * --------------------------------------------------------------------------------------------------------------------
 */

using System;
using System.IO;
using System.Linq;

namespace AI4E.AspNetCore.Blazor.Server
{
    internal class BlazorConfig
    {
        public string SourceMSBuildPath { get; }
        public string SourceOutputAssemblyPath { get; }
        public string? WebRootPath { get; }
        public string DistPath
            => Path.Combine(Path.GetDirectoryName(SourceOutputAssemblyPath)!, "dist");
        public bool EnableAutoRebuilding { get; }
        public bool EnableDebugging { get; }

        public static BlazorConfig Read(string assemblyPath)
        {
            return new BlazorConfig(assemblyPath);
        }

        private BlazorConfig(string assemblyPath)
        {
            // TODO: Instead of assuming the lines are in a specific order, either JSON-encode
            // the whole thing, or at least give the lines key prefixes (e.g., "reload:<someuri>")
            // so we're not dependent on order and all lines being present.

            var configFilePath = Path.ChangeExtension(assemblyPath, ".blazor.config");
            var configLines = File.ReadLines(configFilePath).ToList();
            SourceMSBuildPath = configLines[0];

            if (SourceMSBuildPath == ".")
            {
                SourceMSBuildPath = assemblyPath;
            }

            var sourceMsBuildDir = Path.GetDirectoryName(SourceMSBuildPath);
            SourceOutputAssemblyPath = Path.Combine(sourceMsBuildDir!, configLines[1]);

            var webRootPath = Path.Combine(sourceMsBuildDir!, "wwwroot");
            if (Directory.Exists(webRootPath))
            {
                WebRootPath = webRootPath;
            }

            EnableAutoRebuilding = configLines.Contains("autorebuild:true", StringComparer.Ordinal);
            EnableDebugging = configLines.Contains("debug:true", StringComparer.Ordinal);
        }
    }
}
