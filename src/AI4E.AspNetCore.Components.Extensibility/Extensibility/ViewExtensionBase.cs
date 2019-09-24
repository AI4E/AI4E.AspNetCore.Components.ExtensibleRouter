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

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace AI4E.AspNetCore.Components.Extensibility
{
    /// <summary>
    /// A base type for view extensions.
    /// </summary>
    /// <remarks>
    /// A view extension can alternatively beeing rendered via
    /// the <see cref="ViewExtensionPlaceholder{TViewExtension}"/> component.
    /// </remarks>
    public abstract class ViewExtensionBase : ComponentBase, IViewExtensionDefinition
    {
        private ParameterView _parameters;

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (builder is null)
                throw new System.ArgumentNullException(nameof(builder));

            builder.OpenComponent(sequence: 0, typeof(ViewExtensionPlaceholder<>).MakeGenericType(GetType()));
            builder.AddMultipleAttributes(sequence: 0, _parameters.ToDictionary());
            builder.CloseComponent();
        }

        /// <inheritdoc />
        public override Task SetParametersAsync(ParameterView parameters)
        {
            _parameters = parameters;
            return base.SetParametersAsync(parameters);
        }
    }

    /// <summary>
    /// A generic base type for view extensions.
    /// </summary>
    /// <remarks>
    /// A view extension can alternatively beeing rendered via
    /// the <see cref="ViewExtensionPlaceholder{TViewExtension}"/> component.
    /// </remarks>
    /// <typeparam name="TContext">The type of context parameter.</typeparam>
    public abstract class ViewExtensionBase<TContext> : ViewExtensionBase, IViewExtensionDefinition<TContext>
    {
        /// <inheritdoc />
        [MaybeNull, Parameter] public TContext Context { get; set; } = default!;
    }
}
