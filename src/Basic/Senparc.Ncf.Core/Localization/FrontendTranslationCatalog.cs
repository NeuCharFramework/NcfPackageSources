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

    private static string LegacyNeutralSource(string value)
    {
        var parts = SplitLegacy(value);
        return parts.Length == 2 ? parts[0] : value ?? string.Empty;
    }

    private static string LegacyLocalizedTarget(string value)
    {
        var parts = SplitLegacy(value);
        return parts.Length == 2 ? parts[1] : value ?? string.Empty;
    }

    private static string[] SplitLegacy(string value) =>
        (value ?? string.Empty).Split(new[] { "||" }, 2, StringSplitOptions.None);

    private static bool IsFrontendKey(LocalizedString item) =>
        item.Name.StartsWith("Frontend.", StringComparison.Ordinal)
        || item.Name.StartsWith("Global.", StringComparison.Ordinal);
}
