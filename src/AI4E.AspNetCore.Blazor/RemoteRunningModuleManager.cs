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
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using AI4E.Modularity;
using AI4E.Modularity.Metadata;
using AI4E.Utils;
using AI4E.Utils.Async;
using Microsoft.Extensions.Logging;

namespace AI4E.AspNetCore.Blazor
{
    // TODO: This is copied from AI4E.Modularity.Host.RunningModuleManager
    //       Can we reduce the code duplication?
    public sealed class RemoteRunningModuleManager : IRunningModuleManager, IAsyncInitialization, IDisposable
    {
        private ImmutableList<ModuleIdentifier> _modules = ImmutableList<ModuleIdentifier>.Empty;
        private readonly object _mutex = new object();

        private readonly AsyncInitializationHelper _initializationHelper;
        private readonly IMessageDispatcher _messageDispatcher;

        public RemoteRunningModuleManager(IMessageDispatcher messageDispatcher)
        {
            if (messageDispatcher == null)
                throw new ArgumentNullException(nameof(messageDispatcher));

            _messageDispatcher = messageDispatcher;
            _initializationHelper = new AsyncInitializationHelper(InitiallyLoadModules);
        }

        private async Task InitiallyLoadModules(CancellationToken cancellation)
        {
            var dispatchResult = await _messageDispatcher.QueryAsync<RunningModules>(cancellation);

            if (dispatchResult.IsSuccessWithResult<RunningModules>(out var runningModules))
            {
                foreach (var module in runningModules.Modules)
                {
                    Started(module);
                }
            }
        }

        // TODO: This should be internal, but we have to split the interface to do this.
        public void Started(ModuleIdentifier module)
        {
            bool added;

            lock (_mutex)
            {
                added = !_modules.Contains(module);

                if (added)
                {
                    _modules = _modules.Add(module);
                }
            }

            if (added)
            {
                ModuleStarted?.InvokeAll(this, module);
            }
        }

        // TODO: This should be internal, but we have to split the interface to do this.
        public void Terminated(ModuleIdentifier module)
        {
            bool removed;

            lock (_mutex)
            {
                var modules = _modules;
                _modules = _modules.Remove(module);

                removed = modules != _modules;
            }

            if (removed)
            {
                ModuleTerminated?.InvokeAll(this, module);
            }
        }

        public IReadOnlyCollection<ModuleIdentifier> Modules => Volatile.Read(ref _modules);

        public event EventHandler<ModuleIdentifier>? ModuleStarted;
        public event EventHandler<ModuleIdentifier>? ModuleTerminated;

        public Task Initialization => _initializationHelper.Initialization;

        public void Dispose()
        {
            _initializationHelper.Cancel();
        }
    }

#pragma warning disable CA1812
    [MessageHandler]
    internal sealed class RunningModuleEventHandler
#pragma warning restore CA1812
    {
        private readonly IRunningModuleManager _runningModuleManager;
        private readonly ILogger<RunningModuleEventHandler>? _logger;

        public RunningModuleEventHandler(
            IRunningModuleManager runningModuleManager,
            ILogger<RunningModuleEventHandler>? logger = null)
        {
            if (runningModuleManager == null)
                throw new ArgumentNullException(nameof(runningModuleManager));

            _runningModuleManager = runningModuleManager;
            _logger = logger;
        }

        public void Handle(ModuleStartedEvent eventMessage)
        {
            _logger?.LogDebug($"Module {eventMessage.Module.Name} is reported as running.");
            _runningModuleManager.Started(eventMessage.Module);
        }

        public void Handle(ModuleTerminatedEvent eventMessage)
        {
            _logger?.LogDebug($"Module {eventMessage.Module.Name} is reported as terminated.");
            _runningModuleManager.Terminated(eventMessage.Module);
        }
    }
}
