/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: FrontendTranslationCatalog.cs
    File description: FrontendTranslationCatalog related implementation

    Created by: Senparc - 20261116

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261124
    Change description: v0.31.1-preview12 Improved frontend localization catalog filtering and fallback behavior.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Localization;

namespace Senparc.Ncf.Core.Localization;

/// <summary>
/// Builds frontend source-text translations from neutral and culture-specific
/// resource entries. Each culture resource stores one final-language value.
/// The legacy <c>source||target</c> format remains readable for compatibility
/// with packages that have not yet migrated their resources.
/// </summary>
public static class FrontendTranslationCatalog
{
    public static IReadOnlyDictionary<string, string> Get(
        IStringLocalizerFactory factory,
        Type resourceType)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(resourceType);

        var activeCulture = CultureInfo.CurrentUICulture;
        var neutral = GetStrings(factory, resourceType, CultureInfo.InvariantCulture)
            .Where(IsFrontendKey)
            .ToDictionary(
                item => item.Name,
                item => LegacyNeutralSource(item.Value),
                StringComparer.Ordinal);

        var result = neutral.Values
            .GroupBy(source => source, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.Ordinal);
        foreach (var entry in neutral)
        {
            result[entry.Key] = entry.Value;
        }

        var localized = factory.Create(resourceType)
            .GetAllStrings(includeParentCultures: false)
            .Where(IsFrontendKey);
        foreach (var item in localized)
        {
            var target = LegacyLocalizedTarget(item.Value);
            if (neutral.TryGetValue(item.Name, out var neutralSource))
            {
                // A culture resource can intentionally repeat the neutral source
                // when a translation is missing. Keep the neutral target instead
                // of letting that placeholder overwrite a real translation from
                // another resource family.
                if (!activeCulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(target, neutralSource, StringComparison.Ordinal))
                {
                    target = neutralSource;
                }

                result[neutralSource] = target;
                result[item.Name] = target;
            }
        }

        if (activeCulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var source in neutral.Values)
            {
                result[source] = source;
            }
            foreach (var entry in neutral)
            {
                result[entry.Key] = entry.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Reads localized resource strings under a temporary UI culture and restores the original culture.
    /// </summary>
    /// <param name="factory">The localizer factory.</param>
    /// <param name="resourceType">The resource type to inspect.</param>
    /// <param name="culture">The culture used for lookup.</param>
    /// <returns>The strings available for the requested culture.</returns>
    private static IReadOnlyList<LocalizedString> GetStrings(
        IStringLocalizerFactory factory,
        Type resourceType,
        CultureInfo culture)
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = culture;
            return factory.Create(resourceType)
                .GetAllStrings(includeParentCultures: false)
                .ToArray();
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    /// <summary>
    /// Extracts the neutral/source side of a legacy <c>source||localized</c> value.
    /// </summary>
    /// <param name="value">The legacy combined value.</param>
    /// <returns>The neutral text.</returns>
    private static string LegacyNeutralSource(string value)
    {
        var parts = SplitLegacy(value);
        return parts.Length == 2 ? parts[0] : value ?? string.Empty;
    }

    /// <summary>
    /// Extracts the localized/target side of a legacy <c>source||localized</c> value.
    /// </summary>
    /// <param name="value">The legacy combined value.</param>
    /// <returns>The localized text.</returns>
    private static string LegacyLocalizedTarget(string value)
    {
        var parts = SplitLegacy(value);
        return parts.Length == 2 ? parts[1] : value ?? string.Empty;
    }

    /// <summary>
    /// Splits a legacy source/localized string at its first <c>||</c> separator.
    /// </summary>
    /// <param name="value">The legacy combined string.</param>
    /// <returns>Up to two string parts.</returns>
    private static string[] SplitLegacy(string value) =>
        (value ?? string.Empty).Split(new[] { "||" }, 2, StringSplitOptions.None);

    /// <summary>
    /// Determines whether a localized resource key belongs to the frontend/global catalog.
    /// </summary>
    /// <param name="item">The localized string metadata.</param>
    /// <returns><see langword="true"/> for Frontend or Global keys.</returns>
    private static bool IsFrontendKey(LocalizedString item) =>
        item.Name.StartsWith("Frontend.", StringComparison.Ordinal)
        || item.Name.StartsWith("Global.", StringComparison.Ordinal);
}
