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
using System.Threading.Tasks;
using AI4E;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Blazor.Hosting
{
    public static class ComponentsModularityWebAssemblyHostExtension
    {
        public static IWebAssemblyHost InitializeApplicationServices(this IWebAssemblyHost webhost)
        {
            if (webhost == null)
                throw new ArgumentNullException(nameof(webhost));

            var serviceProvider = webhost.Services;
            var applicationServiceManager = serviceProvider.GetService<ApplicationServiceManager>();

            if (applicationServiceManager != null)
            {
                async Task InitializeApplicationServicesAsync()
                {
                    await applicationServiceManager.InitializeApplicationServicesAsync(serviceProvider, cancellation: default);

                    // Forces an asynchronous yield to the continuation that blocks synchronously
                    // We do not want the contiuations of applicationServiceManager.InitializeApplicationServicesAsync to be blocked indefinitely
                    await Task.Yield();
                }

                // We cannot wait for the result currently, as this blocks the JsRuntime to be initialized that we need in the app-services.
                // https://github.com/AI4E/AI4E/issues/39
                InitializeApplicationServicesAsync()
                    .ConfigureAwait(false)
                    //.GetAwaiter()
                    //.GetResult()
                    ;
            }

            return webhost;
        }
    }
}
