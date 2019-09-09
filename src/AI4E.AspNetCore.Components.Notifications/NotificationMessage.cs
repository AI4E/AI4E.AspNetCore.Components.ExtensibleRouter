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
using AI4E.Utils;

namespace AI4E.AspNetCore.Components.Notifications
{
    /// <summary>
    /// Represents a notification message.
    /// </summary>
    public sealed class NotificationMessage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="NotificationMessage"/> type.
        /// </summary>
        /// <param name="notificationType">The type of notification.</param>
        /// <param name="message">The notification message.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/>is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="notificationType"/> is an invalid value.
        /// </exception>
        public NotificationMessage(
            NotificationType notificationType,
            string message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            if (!notificationType.IsValid())
            {
                throw new ArgumentException(
                    $"The argument must be one of the values defined in {typeof(NotificationType)}",
                    nameof(notificationType));
            }

            NotificationType = notificationType;
            Message = message;
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
        /// Gets or sets the notification description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the uri of the notification target.
        /// </summary>
        public string? TargetUri { get; set; }

        /// <summary>
        /// Gets or sets the date and time of the notification's expiration
        /// or <c>null</c> if the notification has no expiration.
        /// </summary>
        public DateTime? Expiration { get; set; }

        /// <summary>
        /// Gets or sets a boolean value indicating whether the notification may be dismissed.
        /// </summary>
        public bool AllowDismiss { get; set; }

        /// <summary>
        /// Gets or sets an url filter that specifies on which pages the alert shall be displayed
        /// or <c>null</c> if it shall be displayed on all pages.
        /// </summary>
        public UriFilter UriFilter { get; set; }

        /// <summary>
        /// Gets or sets the notification key.
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the notification.
        /// </summary>
        public DateTime? Timestamp { get; set; }
    }
}
