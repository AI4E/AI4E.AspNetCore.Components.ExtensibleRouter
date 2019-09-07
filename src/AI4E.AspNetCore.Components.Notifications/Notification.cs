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
using System.Diagnostics;

namespace AI4E.AspNetCore.Components.Notifications
{
    public readonly struct Notification : INotification, IEquatable<Notification>
    {
        private readonly NotificationManager? _notificationManager;

        internal Notification(
            NotificationManager notificationManager,
            LinkedListNode<NotificationMessage> notificationRef)
        {
            Debug.Assert(notificationManager != null);
            Debug.Assert(notificationRef != null);

            _notificationManager = notificationManager;
            NotificationRef = notificationRef;
        }

        internal LinkedListNode<NotificationMessage>? NotificationRef { get; }

        /// <summary>
        /// Gets the notification manager that manages the notification
        /// or <c>null</c> if the notification is a default value.
        /// </summary>
        public INotificationManager<Notification>? NotificationManager => _notificationManager;

        /// <inheritdoc />
        public bool Equals(Notification other)
        {
            // It is not necessary to include the INotificationManager into comparison.
            // A notification-ref belongs to a single INotificationManager in its complete lifetime,
            // so it should be suffice to compare the nodes.

            return NotificationRef == other.NotificationRef;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is Notification notification && Equals(notification);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // It is not necessary to include the INotificationManager into comparison.
            // A notification-ref belongs to a single INotificationManager in its complete lifetime,
            // so it should be suffice to compare the nodes.

            return NotificationRef?.GetHashCode() ?? 0;
        }

        public static bool operator ==(in Notification left, in Notification right)
        {
            return left.Equals(right);
        }


        public static bool operator !=(in Notification left, in Notification right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc />
        public bool IsExpired => NotificationRef == null || NotificationRef.List == null;

        /// <inheritdoc />
        public NotificationType NotificationType => NotificationRef?.Value.NotificationType ?? NotificationType.None;

        /// <inheritdoc />
        public string Message => NotificationRef?.Value.Message ?? string.Empty;

        /// <inheritdoc />
        public bool AllowDismiss => !IsExpired && (NotificationRef?.Value.AllowDismiss ?? false);

        /// <inheritdoc />
        public string? Key => NotificationRef?.Value.Key;

        /// <inheritdoc />
        public void Dismiss()
        {
            // The notification is either already expired or cannot be dismissed.
            // The notification can never go back to state "non-expired".
            if (!AllowDismiss)
                return;

            _notificationManager?.Dismiss(this);
        }
    }
}
