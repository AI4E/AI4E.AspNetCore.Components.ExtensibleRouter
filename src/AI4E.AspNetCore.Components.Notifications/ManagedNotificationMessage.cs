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
    /// <summary>
    /// Represents a notification message under the control of a notification manager.
    /// </summary>
    internal sealed class ManagedNotificationMessage
    {
        public ManagedNotificationMessage(
            NotificationMessage notificationMessage,
            NotificationManager notificationManager,
            IDateTimeProvider dateTimeProvider)
        {
            if (notificationMessage is null)
                throw new ArgumentNullException(nameof(notificationMessage));

            if (notificationManager is null)
                throw new ArgumentNullException(nameof(notificationManager));

            if (dateTimeProvider is null)
                throw new ArgumentNullException(nameof(dateTimeProvider));

            NotificationManager = notificationManager!;

            NotificationType = notificationMessage.NotificationType;
            Message = notificationMessage.Message;
            Description = notificationMessage.Description;
            TargetUri = notificationMessage.TargetUri;
            Expiration = notificationMessage.Expiration;
            AllowDismiss = notificationMessage.AllowDismiss;
            UriFilter = notificationMessage.UriFilter;
            Key = notificationMessage.Key;
            Timestamp = notificationMessage.Timestamp ?? dateTimeProvider.GetCurrentTime();
        }

        public NotificationManager NotificationManager { get; }

        /// <summary>
        /// Gets the type of notification.
        /// </summary>
        public NotificationType NotificationType { get; }

        /// <summary>
        /// Gets the notification message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the notification description.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the uri of the notification target.
        /// </summary>
        public string? TargetUri { get; }

        /// <summary>
        /// Gets the date and time of the notification's expiration
        /// or <c>null</c> if the notification has no expiration.
        /// </summary>
        public DateTime? Expiration { get; }

        /// <summary>
        /// Gets a boolean value indicating whether the notification may be dismissed.
        /// </summary>
        public bool AllowDismiss { get; }

        /// <summary>
        /// Gets an url filter that specifies on which pages the alert shall be displayed
        /// or <c>null</c> if it shall be displayed on all pages.
        /// </summary>
        public UriFilter UriFilter { get; }

        /// <summary>
        /// Gets the notification key.
        /// </summary>
        public string? Key { get; }

        /// <summary>
        /// Gets the timestamp of the notification.
        /// </summary>
        public DateTime Timestamp { get; }
    }
}
