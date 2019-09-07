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

namespace AI4E.AspNetCore.Components.Notifications
{
    public interface INotification
    {
        /// <summary>
        /// Gets a boolean value indicating whether the notification is expired.
        /// </summary>
        bool IsExpired { get; }

        /// <summary>
        /// Gets the type of notification.
        /// </summary>
        NotificationType NotificationType { get; }

        /// <summary>
        /// Gets the notification message.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets a boolean value indicating whether the notification may be dismissed.
        /// </summary>
        bool AllowDismiss { get; }

        /// <summary>
        /// Gets the notification key.
        /// </summary>
        public string? Key { get; }

        /// <summary>
        /// Dismisses the notification.
        /// </summary>
        void Dismiss();
    }
}
