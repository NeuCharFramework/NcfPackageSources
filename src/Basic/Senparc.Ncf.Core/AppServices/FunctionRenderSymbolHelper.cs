/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FunctionRenderSymbolHelper.cs
    File description: FunctionRenderSymbolHelper related implementation

    Created by: Senparc - 20260327

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Senparc.Ncf.Core.AppServices
{
    /// <summary>
    /// FunctionRender Symbol helper class, responsible for parsing the [#sym:FunctionRender] tag.
    /// </summary>
    public static class FunctionRenderSymbolHelper
    {
        /// <summary>Gets the canonical FunctionRender symbol name.</summary>
        public const string FunctionRenderSymbolName = "FunctionRender";
        /// <summary>Gets the canonical serialized FunctionRender symbol tag.</summary>
        public const string FunctionRenderSymbolTag = "[#sym:FunctionRender]";

        private static readonly Regex SymbolRegex = new Regex(@"\[#sym\s*:\s*(?<name>[A-Za-z0-9_\.\-]+)\s*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Extracts distinct symbol names from all supported symbol tags in the input.
        /// </summary>
        /// <param name="input">The text that may contain symbol tags.</param>
        /// <returns>Distinct symbol names using case-insensitive comparison.</returns>
        public static IReadOnlyList<string> ExtractSymbolNames(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Array.Empty<string>();
            }

            var symbols = SymbolRegex.Matches(input)
                                    .Select(m => m.Groups["name"]?.Value)
                                    .Where(v => !string.IsNullOrWhiteSpace(v))
                                    .Select(v => v.Trim())
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .ToList();

            return symbols;
        }

        /// <summary>
        /// Determines whether the input contains a symbol tag with the specified name.
        /// </summary>
        /// <param name="input">The text to inspect.</param>
        /// <param name="symbolName">The symbol name to find.</param>
        /// <returns><see langword="true"/> when the named symbol is present.</returns>
        public static bool HasSymbol(string input, string symbolName)
        {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(symbolName))
            {
                return false;
            }

            return ExtractSymbolNames(input).Any(z => z.Equals(symbolName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines whether the input contains a FunctionRender symbol tag.
        /// </summary>
        /// <param name="input">The text to inspect.</param>
        /// <returns><see langword="true"/> when a FunctionRender tag is present.</returns>
        public static bool HasFunctionRenderSymbol(string input)
        {
            return HasSymbol(input, FunctionRenderSymbolName);
        }
    }
}
