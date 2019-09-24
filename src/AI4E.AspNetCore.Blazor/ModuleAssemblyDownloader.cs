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
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AI4E.Modularity;
using AI4E.Modularity.Metadata;
using Microsoft.Extensions.Logging;

namespace AI4E.AspNetCore.Blazor
{
#pragma warning disable CA1812
    internal sealed class ModuleAssemblyDownloader : IModuleAssemblyDownloader
#pragma warning restore CA1812
    {
        private readonly HttpClient _httpClient;
        private readonly IModulePropertiesLookup _modulePropertiesLookup;
        private readonly ILogger<ModuleAssemblyDownloader>? _logger;
        private readonly ConcurrentDictionary<string, Assembly> _assemblies = new ConcurrentDictionary<string, Assembly>();

        public ModuleAssemblyDownloader(
            HttpClient httpClient,
            IModulePropertiesLookup modulePropertiesLookup,
            ILogger<ModuleAssemblyDownloader>? logger = null)
        {
            if (httpClient == null)
                throw new ArgumentNullException(nameof(httpClient));

            if (modulePropertiesLookup == null)
                throw new ArgumentNullException(nameof(modulePropertiesLookup));

            _httpClient = httpClient;
            _modulePropertiesLookup = modulePropertiesLookup;
            _logger = logger;
        }

        public Assembly? GetAssembly(string assemblyName)
        {
            if (!_assemblies.TryGetValue(assemblyName, out var assembly))
            {
                assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(p => p.GetName().Name == assemblyName);
            }

            return assembly;
        }

        public async ValueTask<Assembly?> InstallAssemblyAsync(ModuleIdentifier module, string assemblyName, CancellationToken cancellation)
        {
            var result = GetAssembly(assemblyName);
            if (result != null)
            {
                _logger?.LogDebug($"Installing assembly {assemblyName} for module {module}: Already installed.");
                return result;
            }

            _logger?.LogDebug($"Installing assembly {assemblyName} for module {module}.");

            var moduleProperties = await _modulePropertiesLookup.LookupAsync(module, cancellation);

            if (moduleProperties == null)
            {
                _logger?.LogError($"Unable to install assembly {assemblyName} for module {module}. The module properties could not be fetched.");
                return null;
            }

            foreach (var prefix in moduleProperties.Prefixes)
            {
                var assemblyUri = GetAssemblyUri(prefix, assemblyName);
                HttpResponseMessage response;

                try
                {
                    response = await _httpClient.GetAsync(assemblyUri, cancellation).ConfigureAwait(false);
                }
                catch (Exception exc)
                {
                    _logger?.LogWarning(exc, $"Unable to load assembly {assemblyName} from source {assemblyUri}.");
                    continue;
                }

                if (response.IsSuccessStatusCode)
                {
                    var assemblyBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                    try
                    {
                        result = Assembly.Load(assemblyBytes);
                    }
                    catch (Exception exc)
                    {
                        _logger?.LogWarning(exc, $"Unable to install loaded assembly {assemblyName}.");
                        continue;
                    }

                    _logger?.LogDebug($"Successfully installed assembly {assemblyName}. Response status was: {response.StatusCode} {response?.ReasonPhrase ?? string.Empty}.");

                    return result;
                }

                _logger?.LogWarning($"Unable to load assembly {assemblyName} from source {assemblyUri}.");
            }

            if (moduleProperties.Prefixes.Any())
            {
                _logger?.LogError($"Unable to load assembly {assemblyName}. No source successful.");
            }
            else
            {
                _logger?.LogError($"Unable to load assembly {assemblyName}. No sources available.");
            }

            return null;
        }

        private string GetAssemblyUri(string prefix, string assemblyName)
        {
            var assemblyUriBuilder = new StringBuilder();
            var baseAddress = _httpClient.BaseAddress.ToString();

            assemblyUriBuilder.Append(baseAddress);

            if (!baseAddress.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                assemblyUriBuilder.Append('/');
            }

            assemblyUriBuilder.Append(prefix);

            if (!prefix.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                assemblyUriBuilder.Append('/');
            }

            assemblyUriBuilder.Append("_framework/_bin/");
            assemblyUriBuilder.Append(Uri.EscapeDataString(assemblyName));
            assemblyUriBuilder.Append(".dll");

            return assemblyUriBuilder.ToString();
        }
    }
}
