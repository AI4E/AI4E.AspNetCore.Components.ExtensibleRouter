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
using System.Threading;
using System.Threading.Tasks;
using AI4E.Modularity;
using AI4E.Modularity.Metadata;
using AI4E.Routing;
using Microsoft.Extensions.Logging;

namespace AI4E.AspNetCore.Blazor
{
    internal sealed class ModuleManifestProvider : IModuleManifestProvider
    {
        private readonly IModulePropertiesLookup _modulePropertiesLookup;
        private readonly IRemoteMessageDispatcher _messageDispatcher;
        private readonly ILogger<ModuleManifestProvider> _logger;
        private readonly ConcurrentDictionary<ModuleIdentifier, BlazorModuleManifest> _cache;

        public ModuleManifestProvider(
            IModulePropertiesLookup modulePropertiesLookup,
            IRemoteMessageDispatcher messageDispatcher,
            ILogger<ModuleManifestProvider> logger = null)
        {
            if (modulePropertiesLookup == null)
                throw new ArgumentNullException(nameof(modulePropertiesLookup));

            if (messageDispatcher == null)
                throw new ArgumentNullException(nameof(messageDispatcher));

            _modulePropertiesLookup = modulePropertiesLookup;
            _messageDispatcher = messageDispatcher;
            _logger = logger;

            _cache = new ConcurrentDictionary<ModuleIdentifier, BlazorModuleManifest>();
        }

        public ValueTask<BlazorModuleManifest> GetModuleManifestAsync(ModuleIdentifier module, bool bypassCache, CancellationToken cancellation)
        {
            _logger?.LogDebug($"Requesting manifest for module {module}.");

            if (!bypassCache && _cache.TryGetValue(module, out var result))
            {
                _logger?.LogTrace($"Successfully loaded manifest for module {module} from cache.");
                return new ValueTask<BlazorModuleManifest>(result);
            }

            return GetModuleManifestCoreAsync(module, cancellation);
        }

        private async ValueTask<BlazorModuleManifest> GetModuleManifestCoreAsync(ModuleIdentifier module, CancellationToken cancellation)
        {
            var moduleProperties = await _modulePropertiesLookup.LookupAsync(module, cancellation);

            if (moduleProperties == null)
            {
                _logger?.LogError($"Unable to load manifest for {module}. The module properties could not be fetched.");
                return null;
            }

            foreach (var endPoint in moduleProperties.EndPoints)
            {
                var dispatchData = new DispatchDataDictionary<Query<BlazorModuleManifest>>(new Query<BlazorModuleManifest>());
                var queryResult = await _messageDispatcher.DispatchAsync(dispatchData, publish: false, endPoint, cancellation);

                if (!queryResult.IsSuccessWithResult<BlazorModuleManifest>(out var manifest))
                {
                    _logger?.LogWarning($"Unable to load manifest for {module} from end-point {endPoint}.");

                    continue;
                }

                _cache.TryAdd(module, manifest);
                _logger?.LogDebug($"Successfully loaded manifest for module {module}.");
                return manifest;
            }

            if (moduleProperties.EndPoints.Any())
            {
                _logger?.LogError($"Unable to load manifest for {module}. No end-point matched.");
            }
            else
            {
                _logger?.LogError($"Unable to load manifest for {module}. No end-points available.");
            }

            return null;
        }
    }
}
