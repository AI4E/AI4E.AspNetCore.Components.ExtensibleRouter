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

namespace AI4E.AspNetCore.Components.Notifications
{
    public sealed class NotificationManagerScope : INotificationManagerScope
    {
        private readonly HashSet<object> _notifications = new HashSet<object>();
        private bool _isDisposed = false;

        public NotificationManagerScope(INotificationManager notificationManager)
        {
            if (notificationManager is null)
                throw new ArgumentNullException(nameof(notificationManager));

            NotificationManager = notificationManager;
        }

        INotificationManagerScope INotificationManager.CreateScope()
        {
            return new NotificationManagerScope(this);
        }

        INotificationRecorder INotificationManager.CreateRecorder()
        {
            return new NotificationRecorder(this);
        }

        public INotificationManager NotificationManager { get; }

        public NotificationPlacement PlaceNotification(NotificationMessage notificationMessage)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (notificationMessage is null)
                throw new ArgumentNullException(nameof(notificationMessage));

            var placement = NotificationManager.PlaceNotification(notificationMessage);
            _notifications.Add(placement.NotificationRef);

            return new NotificationPlacement(NotificationManager, placement.NotificationRef);
        }

        public void CancelNotification(in NotificationPlacement notificationPlacement)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (notificationPlacement.NotificationManager != this)
            {
                if (notificationPlacement.IsOfScopedNotificationManager(this))
                {
                    notificationPlacement.NotificationManager.CancelNotification(notificationPlacement);
                }
            }
            else if (_notifications.Remove(notificationPlacement.NotificationRef))
            {
                NotificationManager.CancelNotification(
                    new NotificationPlacement(NotificationManager, notificationPlacement.NotificationRef));
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            foreach (var notification in _notifications)
            {
                NotificationManager.CancelNotification(
                    new NotificationPlacement(NotificationManager, notification));
            }

            _notifications.Clear();
        }
    }
}
