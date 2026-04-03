using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Senparc.Ncf.Core.AppServices
{
    /// <summary>
    /// FunctionRender Symbol helper class responsible for parsing [#sym:FunctionRender] tags.
    /// </summary>
    public static class FunctionRenderSymbolHelper
    {
        public const string FunctionRenderSymbolName = "FunctionRender";
        public const string FunctionRenderSymbolTag = "[#sym:FunctionRender]";

        private static readonly Regex SymbolRegex = new Regex(@"\[#sym\s*:\s*(?<name>[A-Za-z0-9_\.\-]+)\s*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Extract all `sym` marker names from the input.
        /// </summary>
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
        /// Determine whether the input contains the specified sym tag.
        /// </summary>
        public static bool HasSymbol(string input, string symbolName)
        {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(symbolName))
            {
                return false;
            }

            return ExtractSymbolNames(input).Any(z => z.Equals(symbolName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines whether the input contains the FunctionRender sym tag.
        /// </summary>
        public static bool HasFunctionRenderSymbol(string input)
        {
            return HasSymbol(input, FunctionRenderSymbolName);
        }
    }
}
