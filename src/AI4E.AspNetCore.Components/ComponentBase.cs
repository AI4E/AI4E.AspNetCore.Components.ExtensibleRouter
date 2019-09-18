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

// TODO: If we have concurrent LoadOperations, how can we guarantee the Model property to be constant within a render-batch?

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AI4E.AspNetCore.Components.Notifications;
using AI4E.DispatchResults;
using AI4E.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace AI4E.AspNetCore.Components
{
    public abstract class ComponentBase<TModel> : ComponentBase, IDisposable
        where TModel : class
    {
        private readonly AsyncLocal<INotificationManager?> _ambientNotifications;
        private readonly AsyncLocal<TModel?> _ambientModel;
        private readonly Lazy<ILogger?> _logger;

        private readonly object _loadModelMutex = new object();

#pragma warning disable IDE0069
        // If _loadModelCancellationSource is null, no operation is in progress currently.
        private CancellationTokenSource? _loadModelCancellationSource;
#pragma warning restore IDE0069
        private INotificationManagerScope? _loadModelNotifications;

        private TModel? _model;

        protected ComponentBase()
        {
            _ambientNotifications = new AsyncLocal<INotificationManager?>();
            _ambientModel = new AsyncLocal<TModel?>();
            _logger = new Lazy<ILogger?>(BuildLogger);

            // These will be set by DI. Just to disable warning here.
            NotificationManager = null!;
            DateTimeProvider = null!;
            ServiceProvider = null!;
        }

        private ILogger? BuildLogger()
        {
            return ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger<ComponentBase<TModel>>();
        }

        protected internal TModel? Model
            => _ambientModel.Value ?? Volatile.Read(ref _model);

        protected internal bool IsLoading
            => Volatile.Read(ref _loadModelCancellationSource) != null;

        protected internal bool IsLoaded
            => !IsLoading && Model != null;

        protected internal INotificationManager Notifications
            => _ambientNotifications.Value ?? NotificationManager;

        [Inject] private NotificationManager NotificationManager { get; set; }
        [Inject] private Notifications.IDateTimeProvider DateTimeProvider { get; set; } // TODO: Replace me!
        [Inject] private IServiceProvider ServiceProvider { get; set; }

        private ILogger? Logger => _logger.Value;

        protected override void OnInitialized()
        {
            LoadModel();
        }

        #region Loading

        protected void LoadModel()
        {
            lock (_loadModelMutex)
            {
                // An operation is in progress currently.
                if (_loadModelCancellationSource != null)
                {
                    try
                    {
                        _loadModelCancellationSource.Cancel();
                    }
                    catch (ObjectDisposedException) { }
                }

                _loadModelCancellationSource = new CancellationTokenSource();
                InternalLoadModelAsync(_loadModelCancellationSource)
                    .HandleExceptions(Logger);
            }
        }

        private async Task InternalLoadModelAsync(CancellationTokenSource cancellationSource)
        {
            using (cancellationSource)
            {
                // Yield back to the caller to leave the mutex as fast as possible.
                await Task.Yield();

                TModel? model = null;
                var notifications = NotificationManager.CreateRecorder();

                try
                {
                    // Set the ambient alert message handler
                    _ambientNotifications.Value = notifications;

                    try
                    {
                        model = await LoadModelAsync(cancellationSource.Token);
                    }
                    finally
                    {
                        // Reset the ambient alert message handler
                        _ambientNotifications.Value = null;
                    }
                }
                finally
                {
                    if (CommitLoadOperation(cancellationSource, model, notifications))
                    {
                        Debug.Assert(model != null);
                        _ambientModel.Value = model;

                        try
                        {
                            OnModelLoaded();
                            await OnModelLoadedAsync();
                            StateHasChanged();
                        }
                        finally
                        {
                            _ambientModel.Value = null;
                        }
                    }
                }
            }
        }

        private bool CommitLoadOperation(
            CancellationTokenSource cancellationSource,
            TModel? model,
            NotificationRecorder notifications)
        {
            if (Volatile.Read(ref _loadModelCancellationSource) != cancellationSource)
                return false;

            lock (_loadModelMutex)
            {
                if (_loadModelCancellationSource != cancellationSource)
                    return false;

                _loadModelCancellationSource = null;

                _loadModelNotifications?.Dispose();
                _loadModelNotifications = notifications;
                notifications.PublishNotifications();

                var success = model != null;

                if (success)
                {
                    _model = model;
                }

                return success;
            }
        }

        protected virtual ValueTask<TModel?> LoadModelAsync(CancellationToken cancellation)
        {
            try
            {
                return new ValueTask<TModel?>(Activator.CreateInstance<TModel>());
            }
            catch (MissingMethodException exc)
            {
                throw new InvalidOperationException(
                    $"Cannot create a model of type {typeof(TModel)}. The type does not have a public default constructor.", exc);
            }
        }

        protected virtual async ValueTask<TModel?> EvaluateLoadResultAsync(IDispatchResult dispatchResult)
        {
            if (IsSuccess(dispatchResult, out var model))
            {
                await OnLoadSuccessAsync(model, dispatchResult);
                return model;
            }

            await EvaluateFailureResultAsync(dispatchResult);
            return null;
        }

        protected virtual ValueTask OnLoadSuccessAsync(TModel model, IDispatchResult dispatchResult)
        {
            return default;
        }

        protected virtual bool IsSuccess(IDispatchResult dispatchResult, out TModel model)
        {
            if (dispatchResult is null)
                throw new ArgumentNullException(nameof(dispatchResult));

            return dispatchResult.IsSuccessWithResult(out model);
        }

        protected virtual void OnModelLoaded() { }

        protected virtual ValueTask OnModelLoadedAsync()
        {
            return default;
        }

        #endregion

        #region Store

        protected virtual ValueTask EvaluateStoreResultAsync(IDispatchResult dispatchResult)
        {
            if (dispatchResult is null)
                throw new ArgumentNullException(nameof(dispatchResult));

            if (dispatchResult.IsSuccess)
            {
                return OnStoreSuccessAsync(Model, dispatchResult);
            }

            return EvaluateFailureResultAsync(dispatchResult);
        }

        protected virtual ValueTask OnStoreSuccessAsync(TModel? model, IDispatchResult dispatchResult)
        {
            var notification = new NotificationMessage(
                NotificationType.Success,
                "Successfully performed operation.")
            {
                Expiration = DateTimeProvider.GetCurrentTime() + TimeSpan.FromSeconds(10)
            };

            Notifications.PlaceNotification(notification);
            return default;
        }

        #endregion

        #region Failure result evaluation

        private ValueTask EvaluateFailureResultAsync(IDispatchResult dispatchResult)
        {
            if (dispatchResult.IsValidationFailed())
            {
                return OnValidationFailureAsync(dispatchResult);
            }

            if (dispatchResult.IsConcurrencyIssue())
            {
                return OnConcurrencyIssueAsync(dispatchResult);
            }

            if (dispatchResult.IsNotFound())
            {
                return OnNotFoundAsync(dispatchResult);
            }

            if (dispatchResult.IsNotAuthenticated())
            {
                return OnNotAuthenticatedAsync(dispatchResult);
            }

            if (dispatchResult.IsNotAuthorized())
            {
                return OnNotAuthorizedAsync(dispatchResult);
            }

            return OnFailureAsync(dispatchResult);
        }

        protected static string GetValidationMessage(IDispatchResult dispatchResult)
        {
            if (dispatchResult.IsAggregateResult(out var aggregateDispatchResult))
            {
                var dispatchResults = aggregateDispatchResult.Flatten().DispatchResults;

                if (dispatchResults.Count() == 1)
                {
                    dispatchResult = dispatchResults.First();
                }
            }

            if (dispatchResult is ValidationFailureDispatchResult && !string.IsNullOrWhiteSpace(dispatchResult.Message))
            {
                return dispatchResult.Message;
            }

            return "Validation failed.";
        }

        protected virtual ValueTask OnValidationFailureAsync(IDispatchResult dispatchResult)
        {
            var validationResults = (dispatchResult as ValidationFailureDispatchResult)
                ?.ValidationResults
                ?? Enumerable.Empty<ValidationResult>();
            var validationMessages = validationResults.Where(p => string.IsNullOrWhiteSpace(p.Member)).Select(p => p.Message);

            if (!validationMessages.Any())
            {
                validationMessages = Enumerable.Repeat(GetValidationMessage(dispatchResult), 1);
            }

            foreach (var validationMessage in validationMessages)
            {
                var alert = new NotificationMessage(NotificationType.Danger, validationMessage);
                Notifications.PlaceNotification(alert);
            }

            return default;
        }

        protected virtual ValueTask OnConcurrencyIssueAsync(IDispatchResult dispatchResult)
        {
            var notification = new NotificationMessage(NotificationType.Danger, "A concurrency issue occured.");
            Notifications.PlaceNotification(notification);
            return default;
        }

        protected virtual ValueTask OnNotFoundAsync(IDispatchResult dispatchResult)
        {
            var notification = new NotificationMessage(NotificationType.Info, "Not found.");
            Notifications.PlaceNotification(notification);
            return default;
        }

        protected virtual ValueTask OnNotAuthenticatedAsync(IDispatchResult dispatchResult)
        {
            var notification = new NotificationMessage(NotificationType.Info, "Not authenticated.");
            Notifications.PlaceNotification(notification);
            return default;
        }

        protected virtual ValueTask OnNotAuthorizedAsync(IDispatchResult dispatchResult)
        {
            var notification = new NotificationMessage(NotificationType.Info, "Not authorized.");
            Notifications.PlaceNotification(notification);
            return default;
        }

        protected virtual ValueTask OnFailureAsync(IDispatchResult dispatchResult)
        {
            var notification = new NotificationMessage(NotificationType.Danger, "An unexpected error occured.");

            Notifications.PlaceNotification(notification);
            return default;
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_loadModelMutex)
                {
                    try
                    {
                        _loadModelCancellationSource?.Cancel();
                    }
                    catch (ObjectDisposedException) { }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
