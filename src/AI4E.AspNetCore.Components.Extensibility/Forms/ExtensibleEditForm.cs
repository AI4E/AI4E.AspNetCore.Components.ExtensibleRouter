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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace AI4E.AspNetCore.Components.Forms
{
    /// <summary>
    /// An edit form that can be extended by <see cref="EditFormExtension"/>s.
    /// </summary>
    public class ExtensibleEditForm : ExtensibleEditFormBase
    {
        private readonly Func<Task> _handleSubmitDelegate; // Cache to avoid per-render allocations
        private ExtensibleEditContext _extensibleEditContext;

        /// <summary>
        /// Creates a new instance of the <see cref="ExtensibleEditForm"/> component.
        /// </summary>
        public ExtensibleEditForm()
        {
            _handleSubmitDelegate = HandleSubmitAsync;
        }


        private EditContext BuildEditContext()
        {
            return EditContext ?? new EditContext(Model);
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
#pragma warning disable IDE0047
            if ((EditContext == null) == (Model == null))
#pragma warning restore IDE0047
            {
                throw new InvalidOperationException($"{nameof(EditForm)} requires a {nameof(Model)} " +
                    $"parameter, or an {nameof(EditContext)} parameter, but not both.");
            }

            // Update _fixedEditContext if we don't have one yet, or if they are supplying a
            // potentially new EditContext, or if they are supplying a different Model
            if (_extensibleEditContext == null)
            {
                _extensibleEditContext = new ExtensibleEditContext(BuildEditContext());
            }
            else if (EditContext != null || Model != _extensibleEditContext.RootEditContext.Model)
            {
                _extensibleEditContext.RootEditContext = BuildEditContext();
            }
        }

#pragma warning disable CA2007
        private async Task HandleSubmitAsync()
        {
            var isValid = _extensibleEditContext.Validate();

            if (isValid)
            {
                if (OnValidSubmit.HasDelegate)
                {

                    await OnValidSubmit.InvokeAsync(_extensibleEditContext.RootEditContext);

                }

                await _extensibleEditContext.OnValidSubmit();
            }
            else
            {
                if (OnInvalidSubmit.HasDelegate)
                {
                    await OnInvalidSubmit.InvokeAsync(_extensibleEditContext.RootEditContext);
                }

                await _extensibleEditContext.OnInvalidSubmit();
            }
        }
#pragma warning restore CA2007

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            var sequence = 0;
            var editContext = _extensibleEditContext.RootEditContext;

            void BuildEditContextCascadingValue(RenderTreeBuilder builder)
            {
                builder.OpenComponent<CascadingValue<EditContext>>(sequence++);
                builder.AddAttribute(sequence++, nameof(CascadingValue<EditContext>.IsFixed), true);
                builder.AddAttribute(sequence++, nameof(CascadingValue<EditContext>.Value), editContext);
                builder.AddAttribute(sequence++, nameof(CascadingValue<EditContext>.ChildContent), ChildContent?.Invoke(_extensibleEditContext));
                builder.CloseComponent();
            }

            void BuildExtensibleEditContextCascadingValue(RenderTreeBuilder builder)
            {
                builder.OpenComponent<CascadingValue<ExtensibleEditContext>>(sequence++);
                builder.AddAttribute(sequence++, nameof(CascadingValue<ExtensibleEditContext>.IsFixed), true);
                builder.AddAttribute(sequence++, nameof(CascadingValue<ExtensibleEditContext>.Value), _extensibleEditContext);
                builder.AddAttribute(sequence++, nameof(CascadingValue<ExtensibleEditContext>.ChildContent), (RenderFragment)BuildEditContextCascadingValue);
                builder.CloseComponent();
            }

            // If editContext changes, tear down and recreate all descendants.
            // This is so we can safely use the IsFixed optimization on CascadingValue,
            // optimizing for the common case where _fixedEditContext never changes.
            builder.OpenRegion(editContext.GetHashCode());

            builder.OpenElement(sequence++, "form");
            builder.AddMultipleAttributes(sequence++, AdditionalAttributes);
            builder.AddAttribute(sequence++, "onsubmit", _handleSubmitDelegate);
            BuildExtensibleEditContextCascadingValue(builder);
            builder.CloseElement();

            builder.CloseRegion();
        }
    }
}
