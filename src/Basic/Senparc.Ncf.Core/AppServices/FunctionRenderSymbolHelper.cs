/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: FunctionRenderSymbolHelper.cs
    File description: FunctionRenderSymbolHelper related implementation
    
    
    Created by: Senparc - 20260327
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
        public const string FunctionRenderSymbolName = "FunctionRender";
        public const string FunctionRenderSymbolTag = "[#sym:FunctionRender]";

        private static readonly Regex SymbolRegex = new Regex(@"\[#sym\s*:\s*(?<name>[A-Za-z0-9_\.\-]+)\s*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Extract the name of all sym tags in the input.
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
        /// checkinputwhether contains the specified sym tag.
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
        /// checkinputwhether contains FunctionRender sym tag.
        /// </summary>
        public static bool HasFunctionRenderSymbol(string input)
        {
            return HasSymbol(input, FunctionRenderSymbolName);
        }
    }
}
