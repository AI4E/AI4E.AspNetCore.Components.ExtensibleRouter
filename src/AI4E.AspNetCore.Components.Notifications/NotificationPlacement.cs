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

namespace AI4E.AspNetCore.Components.Notifications
{
    public readonly struct NotificationPlacement : IEquatable<NotificationPlacement>, IDisposable
    {
        public NotificationPlacement(INotificationManager notificationManager, object notificationRef)
        {
            if (notificationManager is null)
                throw new ArgumentNullException(nameof(notificationManager));

            if (notificationRef is null)
                throw new ArgumentNullException(nameof(notificationRef));

            NotificationManager = notificationManager;
            NotificationRef = notificationRef;
        }

        public INotificationManager NotificationManager { get; }
        public object NotificationRef { get; }

        public void Dispose()
        {
            NotificationManager?.CancelNotification(this);
        }

        public bool Equals(NotificationPlacement other)
        {
            // It is not necessary to include the INotificationManager into comparison.
            // A notification-ref belongs to a single INotificationManager in its complete lifetime,
            // so it should be suffice to compare the nodes.

            return NotificationRef == other.NotificationRef;
        }

#if NETSTD20
        public override bool Equals(object obj)
#else
        public override bool Equals(object? obj)
#endif
        {
            return obj is NotificationPlacement notificationPlacement && Equals(notificationPlacement);
        }

        public override int GetHashCode()
        {
            // It is not necessary to include the INotificationManager into comparison.
            // A notification-ref belongs to a single INotificationManager in its complete lifetime,
            // so it should be suffice to compare the nodes.

            return NotificationRef?.GetHashCode() ?? 0;
        }

        public static bool operator ==(in NotificationPlacement left, in NotificationPlacement right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(in NotificationPlacement left, in NotificationPlacement right)
        {
            return !left.Equals(right);
        }

        internal bool IsOfScopedNotificationManager(INotificationManager notificationManager)
        {
            var current = NotificationManager;

            while (current is INotificationManagerScope scope)
            {
                current = scope.NotificationManager;

                if (current == notificationManager)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
