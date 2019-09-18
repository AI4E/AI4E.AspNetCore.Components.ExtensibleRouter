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
using System.Threading;
using System.Threading.Tasks;
using AI4E.Modularity;
using AI4E.Modularity.Metadata;
using Microsoft.Extensions.Logging;
using static System.Diagnostics.Debug;

namespace AI4E.AspNetCore.Blazor
{
    public sealed class RemoteModulePropertiesLookup : IModulePropertiesLookup
    {
        private readonly IMessageDispatcher _messageDispatcher;
        private readonly ILogger<RemoteModulePropertiesLookup> _logger;

        private readonly ConcurrentDictionary<ModuleIdentifier, ModuleProperties> _cache;

        public RemoteModulePropertiesLookup(IMessageDispatcher messageDispatcher, ILogger<RemoteModulePropertiesLookup> logger = null)
        {
            if (messageDispatcher == null)
                throw new ArgumentNullException(nameof(messageDispatcher));

            _messageDispatcher = messageDispatcher;
            _logger = logger;

            _cache = new ConcurrentDictionary<ModuleIdentifier, ModuleProperties>();
        }

        public ValueTask<ModuleProperties> LookupAsync(ModuleIdentifier module, CancellationToken cancellation)
        {
            if (module == default)
                throw new ArgumentDefaultException(nameof(module));

            if (_cache.TryGetValue(module, out var moduleProperties))
            {
                return new ValueTask<ModuleProperties>(moduleProperties);
            }

            return InternalLookupAsync(module, cancellation);
        }

        private async ValueTask<ModuleProperties> InternalLookupAsync(ModuleIdentifier module, CancellationToken cancellation)
        {
            var maxTries = 10;
            var timeToWait = new TimeSpan(TimeSpan.TicksPerSecond * 2);

            for (var i = 0; i < maxTries; i++)
            {
                var query = new ModulePropertiesQuery(module);
                var queryResult = await _messageDispatcher.DispatchAsync(query, cancellation);

                if (queryResult.IsSuccessWithResult<ModuleProperties>(out var moduleProperties))
                {
                    Assert(moduleProperties != null);
                    return _cache.GetOrAdd(module, moduleProperties);
                }

                if (!queryResult.IsNotFound())
                {
                    _logger?.LogError($"Unable to lookup end-point for module '{module.Name}' for reason: {(queryResult.IsSuccess ? "Wrong type returned" : queryResult.ToString())}.");
                    break;
                }

                await Task.Delay(timeToWait, cancellation);
            }

            return null;
        }
    }
}
