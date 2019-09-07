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
    /// Represents a notification message.
    /// </summary>
    public readonly struct NotificationMessage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="NotificationMessage"/> type.
        /// </summary>
        /// <param name="notificationType">A value of <see cref="NotificationMessage"/> indicating the type of notification.</param>
        /// <param name="message">A <see cref="string"/> specifying the notification message.</param>
        /// <param name="expiration">
        /// The <see cref="DateTime"/> indicating the notification's expiration or <c>null</c> if the notification has no expiration.
        /// </param>
        /// <param name="allowDismiss">A boolean value indicating whether the notification may be dismissed.</param>
        /// <param name="uriFilter">
        /// An <see cref="UriFilter"/> that represents an uri filter that specifies on which
        /// pages the alert shall be displayed.
        /// </param>
        /// <param name="key">The notification key or <c>null</c>.</param>
        public NotificationMessage(
            NotificationType notificationType,
            string message,
            DateTime? expiration = null,
            bool allowDismiss = false,
            UriFilter uriFilter = default,
            string? key = default)
        {
            NotificationType = notificationType;
            Message = message;
            Expiration = expiration;
            AllowDismiss = allowDismiss;
            UriFilter = uriFilter;
            Key = key;
        }

        /// <summary>
        /// Gets the type of notification.
        /// </summary>
        public NotificationType NotificationType { get; }

        /// <summary>
        /// Gets the notification message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the date and time of the notification's expiration or <c>null</c> if the notification has no expiration.
        /// </summary>
        public DateTime? Expiration { get; }

        /// <summary>
        /// Gets a boolean value indicating whether the notification may be dismissed.
        /// </summary>
        public bool AllowDismiss { get; }

        /// <summary>
        /// Gets an url filter that specifies on which pages the alert shall be displayed or <c>null</c> if it shall be displayed on all pages.
        /// </summary>
        public UriFilter UriFilter { get; }

        /// <summary>
        /// Gets the notification key.
        /// </summary>
        public string? Key { get; }
    }
}
