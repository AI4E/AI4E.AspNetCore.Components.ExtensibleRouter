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
using System.Net.Mime;
using AI4E.AspNetCore.Blazor.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Builder
{
    public static class ComponentsModuleServerApplicationBuilderExtension
    {
        public static IApplicationBuilder UseBlazorModule<TProgram>(this IApplicationBuilder applicationBuilder)
        {
            if (applicationBuilder == null)
                throw new ArgumentNullException(nameof(applicationBuilder));

            var clientAssemblyInServerBinDir = typeof(TProgram).Assembly;
            return applicationBuilder.UseBlazorModule(new BlazorOptions
            {
                ClientAssemblyPath = clientAssemblyInServerBinDir.Location,
            });
        }

        // TODO: Test whether publishing works correctly.
        public static IApplicationBuilder UseBlazorModule(this IApplicationBuilder applicationBuilder, BlazorOptions options)
        {
            if (applicationBuilder == null)
                throw new ArgumentNullException(nameof(applicationBuilder));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            // TODO: Make the .blazor.config file contents sane
            // Currently the items in it are bizarre and don't relate to their purpose,
            // hence all the path manipulation here. We shouldn't be hardcoding 'dist' here either.
            var env = applicationBuilder.ApplicationServices.GetService<IHostingEnvironment>();
            var config = BlazorConfig.Read(options.ClientAssemblyPath);

            //if (env.IsDevelopment() && config.EnableAutoRebuilding)
            //{
            //    if (env.ApplicationName.Equals(DevServerApplicationName, StringComparison.OrdinalIgnoreCase))
            //    {
            //        app.UseDevServerAutoRebuild(config);
            //    }
            //    else
            //    {
            //        app.UseHostedAutoRebuild(config, env.ContentRootPath);
            //    }
            //}

            // First, match the request against files in the client app dist directory
            if (Directory.Exists(config.DistPath))
            {
                applicationBuilder.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(config.DistPath),
                    ContentTypeProvider = CreateContentTypeProvider(config.EnableDebugging),
                    OnPrepareResponse = CacheHeaderSettings.SetCacheHeaders,
                });
            }

            // * Before publishing, we serve the wwwroot files directly from source
            //   (and don't require them to be copied into dist).
            //   In this case, WebRootPath will be nonempty if that directory exists.
            // * After publishing, the wwwroot files are already copied to 'dist' and
            //   will be served by the above middleware, so we do nothing here.
            //   In this case, WebRootPath will be empty (the publish process sets this).
            if (!string.IsNullOrEmpty(config.WebRootPath))
            {
                applicationBuilder.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(config.WebRootPath),
                    OnPrepareResponse = CacheHeaderSettings.SetCacheHeaders,
                });
            }

            return applicationBuilder;
        }

        private static IContentTypeProvider CreateContentTypeProvider(bool enableDebugging)
        {
            var result = new FileExtensionContentTypeProvider();
            AddMapping(result, ".dll", MediaTypeNames.Application.Octet);

            if (enableDebugging)
            {
                AddMapping(result, ".pdb", MediaTypeNames.Application.Octet);
            }

            return result;
        }

        private static void AddMapping(FileExtensionContentTypeProvider provider, string name, string mimeType)
        {
            if (!provider.Mappings.ContainsKey(name))
            {
                provider.Mappings.Add(name, mimeType);
            }
        }
    }

    internal static class CacheHeaderSettings
    {
        internal static void SetCacheHeaders(StaticFileResponseContext ctx)
        {
            // By setting "Cache-Control: no-cache", we're allowing the browser to store
            // a cached copy of the response, but telling it that it must check with the
            // server for modifications (based on Etag) before using that cached copy.
            // Longer term, we should generate URLs based on content hashes (at least
            // for published apps) so that the browser doesn't need to make any requests
            // for unchanged files.
            var headers = ctx.Context.Response.GetTypedHeaders();
            if (headers.CacheControl == null)
            {
                headers.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true
                };
            }
        }
    }
}
