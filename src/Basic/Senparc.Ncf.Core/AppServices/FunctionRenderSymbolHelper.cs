using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Senparc.Ncf.Core.AppServices
{
    /// <summary>
    /// FunctionRender 符号辅助类，负责解析 [#sym:FunctionRender] 标记。
    /// </summary>
    public static class FunctionRenderSymbolHelper
    {
        public const string FunctionRenderSymbolName = "FunctionRender";
        public const string FunctionRenderSymbolTag = "[#sym:FunctionRender]";

        private static readonly Regex SymbolRegex = new Regex(@"\[#sym\s*:\s*(?<name>[A-Za-z0-9_\.\-]+)\s*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 提取输入中的所有 sym 标记名称。
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
        /// 判断输入是否包含指定 sym 标记。
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
        /// 判断输入是否包含 FunctionRender sym 标记。
        /// </summary>
        public static bool HasFunctionRenderSymbol(string input)
        {
            return HasSymbol(input, FunctionRenderSymbolName);
        }
    }
}
