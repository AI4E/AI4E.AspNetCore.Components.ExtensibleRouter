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

/* Based on
* --------------------------------------------------------------------------------------------------------------------
* AspNet Core (https://github.com/aspnet/AspNetCore)
* Copyright (c) .NET Foundation. All rights reserved.
* Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
* --------------------------------------------------------------------------------------------------------------------
*/

using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace AI4E.AspNetCore.Components.Forms
{
    /// <summary>
    /// Extensions an <see cref="ExtensibleEditForm"/>.
    /// </summary>
    public class EditFormExtension : ExtensibleEditFormBase, IDisposable
    {
        private EditContext _fixedEditContext;
        private FormExtension? _registration;

        /// <summary>
        /// Gets or sets the cascading extendible edit context.
        /// </summary>
        [CascadingParameter] public ExtensibleEditContext ExtensibleEditContext { get; set; }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
#pragma warning disable IDE0047
            if ((EditContext == null) == (Model == null))
#pragma warning restore IDE0047
            {
                throw new InvalidOperationException($"{nameof(EditFormExtension)} requires a {nameof(Model)} " +
                    $"parameter, or an {nameof(EditContext)} parameter, but not both.");
            }

            if (ExtensibleEditContext == null)
            {
                throw new InvalidOperationException("The ExtensibleEditContext parameter must be set.");
            }

            // Update _fixedEditContext if we don't have one yet, or if they are supplying a
            // potentially new EditContext, or if they are supplying a different Model
            if (_fixedEditContext == null || EditContext != null || Model != _fixedEditContext.Model)
            {
                if (_registration != null)
                {
                    ExtensibleEditContext.UnregisterEditFormExtension(_registration.Value);
                }

                _fixedEditContext = EditContext ?? new EditContext(Model);
                _registration = ExtensibleEditContext.RegisterEditFormExtension(_fixedEditContext, OnInvalidSubmit, OnValidSubmit);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

#pragma warning disable IDE0060
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">A boolean value indicating whether the instance is disposing.</param>
        protected virtual void Dispose(bool disposing)
#pragma warning restore IDE0060
        {
            if (_registration != null)
            {
                ExtensibleEditContext.UnregisterEditFormExtension(_registration.Value);
            }
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            var sequence = 0;

            void BuildEditContextCascadingValue(RenderTreeBuilder builder)
            {
                builder.OpenComponent<CascadingValue<EditContext>>(sequence++);
                builder.AddAttribute(sequence++, nameof(CascadingValue<EditContext>.IsFixed), true);
                builder.AddAttribute(sequence++, nameof(CascadingValue<EditContext>.Value), _fixedEditContext);
                builder.AddAttribute(sequence++, nameof(CascadingValue<EditContext>.ChildContent), ChildContent?.Invoke(ExtensibleEditContext));
                builder.CloseComponent();
            }

            // If _fixedEditContext changes, tear down and recreate all descendants.
            // This is so we can safely use the IsFixed optimization on CascadingValue,
            // optimizing for the common case where _fixedEditContext never changes.
            builder.OpenRegion(_fixedEditContext.GetHashCode());

            builder.OpenElement(sequence++, "div");
            builder.AddMultipleAttributes(sequence++, AdditionalAttributes);
            BuildEditContextCascadingValue(builder);
            builder.CloseElement();

            builder.CloseRegion();
        }
    }
}
