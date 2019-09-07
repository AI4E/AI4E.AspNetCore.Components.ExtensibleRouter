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
using System.Collections.Generic;

namespace AI4E.AspNetCore.Components.Routing
{
    internal class RouteEntry
    {
        public RouteEntry(RouteTemplate template, Type handler, string[] unusedRouteParameterNames)
        {
            Template = template;
            UnusedRouteParameterNames = unusedRouteParameterNames;
            Handler = handler;
        }

        public RouteTemplate Template { get; }

        public string[] UnusedRouteParameterNames { get; }

        public Type Handler { get; }

        internal void Match(RouteContext context)
        {
            if (Template.Segments.Length != context.Segments.Length)
            {
                return;
            }

            // Parameters will be lazily initialized.
            Dictionary<string, object> parameters = null;
            for (var i = 0; i < Template.Segments.Length; i++)
            {
                var segment = Template.Segments[i];
                var pathSegment = context.Segments[i];
                if (!segment.Match(pathSegment, out var matchedParameterValue))
                {
                    return;
                }
                else
                {
                    if (segment.IsParameter)
                    {
                        parameters ??= new Dictionary<string, object>(StringComparer.Ordinal);
                        parameters[segment.Value] = matchedParameterValue;
                    }
                }
            }

            // In addition to extracting parameter values from the URL, each route entry
            // also knows which other parameters should be supplied with null values. These
            // are parameters supplied by other route entries matching the same handler.
            if (UnusedRouteParameterNames.Length > 0)
            {
                parameters ??= new Dictionary<string, object>(StringComparer.Ordinal);
                for (var i = 0; i < UnusedRouteParameterNames.Length; i++)
                {
                    parameters[UnusedRouteParameterNames[i]] = null;
                }
            }

            context.Parameters = parameters;
            context.Handler = Handler;
        }
    }
}
