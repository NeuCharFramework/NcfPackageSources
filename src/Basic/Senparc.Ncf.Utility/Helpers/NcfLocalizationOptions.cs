using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Senparc.Ncf.Utility.Helpers
{
    /// <summary>
    /// NCF 全局支持的界面语言。
    /// </summary>
    /// <remarks>
    /// 所有宿主和模块都应从此处读取支持列表，避免语言切换器、请求中间件和资源文件
    /// 分别维护不同的语言集合。
    /// </remarks>
    public static class NcfLocalizationOptions
    {
        public const string DefaultCulture = "zh-CN";

        private static readonly string[] _supportedCultures =
        {
            DefaultCulture,
            "en",
            "ja",
            "fr",
            "es",
            "ru"
        };

        public static IReadOnlyList<string> SupportedCultures { get; } =
            Array.AsReadOnly(_supportedCultures);

        /// <summary>
        /// 将浏览器或调用方传入的区域名称规范化为系统支持的区域名称。
        /// </summary>
        public static bool TryNormalizeCulture(string cultureName, out string normalizedCulture)
        {
            normalizedCulture = DefaultCulture;

            if (string.IsNullOrWhiteSpace(cultureName))
            {
                return false;
            }

            var exactMatch = _supportedCultures.FirstOrDefault(culture =>
                string.Equals(culture, cultureName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
            {
                normalizedCulture = exactMatch;
                return true;
            }

            try
            {
                var requestedCulture = CultureInfo.GetCultureInfo(cultureName.Trim());
                var languageMatch = _supportedCultures.FirstOrDefault(culture =>
                    string.Equals(
                        CultureInfo.GetCultureInfo(culture).TwoLetterISOLanguageName,
                        requestedCulture.TwoLetterISOLanguageName,
                        StringComparison.OrdinalIgnoreCase));

                if (languageMatch != null)
                {
                    normalizedCulture = languageMatch;
                    return true;
                }
            }
            catch (CultureNotFoundException)
            {
                // Invalid culture names are rejected by returning false.
            }

            return false;
        }

        public static SystemLanguage GetSystemLanguage(CultureInfo cultureInfo)
        {
            var languageName = (cultureInfo ?? CultureInfo.CurrentUICulture)
                .TwoLetterISOLanguageName;

            return languageName.ToLowerInvariant() switch
            {
                "zh" => SystemLanguage.Chinese,
                "ja" => SystemLanguage.Japanese,
                "fr" => SystemLanguage.French,
                "es" => SystemLanguage.Spanish,
                "ru" => SystemLanguage.Russian,
                _ => SystemLanguage.English
            };
        }
    }
}
