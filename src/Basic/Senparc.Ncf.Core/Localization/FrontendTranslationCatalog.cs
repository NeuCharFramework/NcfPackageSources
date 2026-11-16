using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Localization;

namespace Senparc.Ncf.Core.Localization;

/// <summary>
/// Builds frontend source-text translations from neutral and culture-specific
/// resource entries. Frontend resource values use the format
/// <c>neutral-source||localized-target</c>.
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
            .Where(item => item.Name.StartsWith("Frontend.", StringComparison.Ordinal))
            .Select(item => new
            {
                item.Name,
                Parts = Split(item.Value)
            })
            .Where(item => item.Parts.Length == 2)
            .ToDictionary(item => item.Name, item => item.Parts, StringComparer.Ordinal);

        var result = neutral.Values
            .GroupBy(parts => parts[0], StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First()[1],
                StringComparer.Ordinal);

        var localized = factory.Create(resourceType)
            .GetAllStrings(includeParentCultures: false)
            .Where(item => item.Name.StartsWith("Frontend.", StringComparison.Ordinal));
        foreach (var item in localized)
        {
            var parts = Split(item.Value);
            if (neutral.TryGetValue(item.Name, out var neutralParts))
            {
                result[neutralParts[0]] = parts.Length == 2 ? parts[1] : item.Value;
            }
            else if (parts.Length == 2 && !result.ContainsKey(parts[0]))
            {
                result[parts[0]] = parts[1];
            }
        }

        if (activeCulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var parts in neutral.Values)
            {
                result[parts[0]] = parts[0];
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

    private static string[] Split(string value) =>
        (value ?? string.Empty).Split(new[] { "||" }, 2, StringSplitOptions.None);
}
