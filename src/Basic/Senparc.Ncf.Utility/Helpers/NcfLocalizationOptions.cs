/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfLocalizationOptions.cs
    File description: NcfLocalizationOptions related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.21.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.21.1-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Senparc.Ncf.Utility.Helpers
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    /// <remarks>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
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
