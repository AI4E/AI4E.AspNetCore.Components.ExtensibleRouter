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

namespace AI4E.AspNetCore.Components.Routing
{
    // This implementation is temporary, in the future we'll want to have
    // a more performant/properly designed routing set of abstractions.
    // To be more precise these are some things we are scoping out:
    // * We are not doing link generation.
    // * We are not supporting all the route constraint formats supported by ASP.NET server-side routing.
    // The class in here just takes care of parsing a route and extracting
    // simple parameters from it.
    // Some differences with ASP.NET Core routes are:
    // * We don't support catch all parameter segments.
    // * We don't support optional parameter segments.
    // * We don't support complex segments.
    // The things that we support are:
    // * Literal path segments. (Like /Path/To/Some/Page)
    // * Parameter path segments (Like /Customer/{Id}/Orders/{OrderId})
    internal class TemplateParser
    {
        public static readonly char[] InvalidParameterNameCharacters =
            new char[] { '*', '?', '{', '}', '=', '.' };

        internal static RouteTemplate ParseTemplate(string template)
        {
            var originalTemplate = template;
            template = template.Trim('/');
            if (template.Length == 0)
            {
                // Special case "/";
                return new RouteTemplate("/", Array.Empty<TemplateSegment>());
            }

            var segments = template.Split('/');
            var templateSegments = new TemplateSegment[segments.Length];
            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (string.IsNullOrEmpty(segment))
                {
                    throw new InvalidOperationException(
                        $"Invalid template '{template}'. Empty segments are not allowed.");
                }

                if (segment[0] != '{')
                {
                    if (segment[segment.Length - 1] == '}')
                    {
                        throw new InvalidOperationException(
                            $"Invalid template '{template}'. Missing '{{' in parameter segment '{segment}'.");
                    }
                    templateSegments[i] = new TemplateSegment(originalTemplate, segment, isParameter: false);
                }
                else
                {
                    if (segment[segment.Length - 1] != '}')
                    {
                        throw new InvalidOperationException(
                            $"Invalid template '{template}'. Missing '}}' in parameter segment '{segment}'.");
                    }

                    if (segment.Length < 3)
                    {
                        throw new InvalidOperationException(
                            $"Invalid template '{template}'. Empty parameter name in segment '{segment}' is not allowed.");
                    }

                    var invalidCharacter = segment.IndexOfAny(InvalidParameterNameCharacters, 1, segment.Length - 2);
                    if (invalidCharacter != -1)
                    {
                        throw new InvalidOperationException(
                            $"Invalid template '{template}'. The character '{segment[invalidCharacter]}' in parameter segment '{segment}' is not allowed.");
                    }

                    templateSegments[i] = new TemplateSegment(originalTemplate, segment.Substring(1, segment.Length - 2), isParameter: true);
                }
            }

            for (var i = 0; i < templateSegments.Length; i++)
            {
                var currentSegment = templateSegments[i];
                if (!currentSegment.IsParameter)
                {
                    continue;
                }

                for (var j = i + 1; j < templateSegments.Length; j++)
                {
                    var nextSegment = templateSegments[j];
                    if (!nextSegment.IsParameter)
                    {
                        continue;
                    }

                    if (string.Equals(currentSegment.Value, nextSegment.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"Invalid template '{template}'. The parameter '{currentSegment}' appears multiple times.");
                    }
                }
            }

            return new RouteTemplate(template, templateSegments);
        }
    }
}
