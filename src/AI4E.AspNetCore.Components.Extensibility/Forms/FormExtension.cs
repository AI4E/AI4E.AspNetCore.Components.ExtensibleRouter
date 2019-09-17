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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace AI4E.AspNetCore.Components.Forms
{
    /// <summary>
    /// A handle for a form-extension.
    /// </summary>
#pragma warning disable CA1815
    public readonly struct FormExtension : IDisposable
#pragma warning restore CA1815
    {
        internal FormExtension(
            ExtensibleEditContext extensibleEditContext,
            EditContext editContext,
            EventCallback<EditContext> onInvalidSubmit,
            EventCallback<EditContext> onValidSubmit,
            int seqNum)
        {
            if (extensibleEditContext is null)
                throw new ArgumentNullException(nameof(extensibleEditContext));

            if (editContext is null)
                throw new ArgumentNullException(nameof(editContext));

            ExtensibleEditContext = extensibleEditContext;
            EditContext = editContext;
            OnInvalidSubmit = onInvalidSubmit;
            OnValidSubmit = onValidSubmit;
            SeqNum = seqNum;
        }


        /// <summary>
        /// Gets the edit-context of the form-extension.
        /// </summary>
        public EditContext EditContext { get; }

        /// <summary>
        /// Gets the event-callback that shall be invoked on invalid submits.
        /// </summary>
        public EventCallback<EditContext> OnInvalidSubmit { get; }

        /// <summary>
        /// Gets the event-callback that shall be invoked on valid submits.
        /// </summary>
        public EventCallback<EditContext> OnValidSubmit { get; }

        internal ExtensibleEditContext ExtensibleEditContext { get; }
        internal int SeqNum { get; }

        /// <summary>
        /// Unregistered the form-extension from the respective edit-context.
        /// </summary>
        public void Dispose()
        {
            if (EditContext != null)
                ExtensibleEditContext.UnregisterEditFormExtension(this);
        }
    }
}
