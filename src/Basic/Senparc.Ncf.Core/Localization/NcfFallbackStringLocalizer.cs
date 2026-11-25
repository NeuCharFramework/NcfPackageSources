/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfFallbackStringLocalizer.cs
    File description: Related implementation.

    Created by: Senparc - 20261110

    Modified by: Senparc - 20261111
    Change description: v0.30.4-preview10 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Reflection;
using Microsoft.Extensions.Localization;

namespace Senparc.Ncf.Core.Localization;

/// <summary>
/// Resolves a resource using the current UI culture, then English, then Chinese.
/// The neutral resource remains the final fallback so existing packages that only
/// carry a neutral Chinese resource continue to work.
/// </summary>
internal sealed class NcfFallbackStringLocalizer : IStringLocalizer
{
    private readonly ResourceManager _resourceManager;
    private readonly string _searchedLocation;

    /// <summary>
    /// Initializes a localizer backed by a resource manager and records the location used
    /// in <see cref="LocalizedString.SearchedLocation"/>.
    /// </summary>
    /// <param name="resourceManager">The resource manager used to load localized resource sets.</param>
    /// <param name="searchedLocation">The assembly or logical location reported for lookups.</param>
    public NcfFallbackStringLocalizer(ResourceManager resourceManager, string searchedLocation)
    {
        _resourceManager = resourceManager;
        _searchedLocation = searchedLocation;
    }

    /// <summary>
    /// Gets a localized value for a resource name using the configured culture fallback chain.
    /// </summary>
    /// <param name="name">The resource key to resolve.</param>
    /// <returns>A localized string, or a resource-not-found result whose value is the key.</returns>
    public LocalizedString this[string name] =>
        Resolve(name, null);

    /// <summary>
    /// Gets a localized value and formats it with the supplied arguments using the current culture.
    /// </summary>
    /// <param name="name">The resource key to resolve.</param>
    /// <param name="arguments">The format arguments applied when the resource exists.</param>
    /// <returns>The resolved and formatted localized string.</returns>
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var localized = Resolve(name, arguments);
            if (localized.ResourceNotFound || arguments is null || arguments.Length == 0)
            {
                return localized;
            }

            return new LocalizedString(
                localized.Name,
                string.Format(CultureInfo.CurrentCulture, localized.Value, arguments),
                localized.ResourceNotFound,
                localized.SearchedLocation);
        }
    }

    /// <summary>
    /// Enumerates the merged resource keys from the current UI culture and configured fallbacks.
    /// When duplicate keys exist, the first value in the fallback order wins.
    /// </summary>
    /// <param name="includeParentCultures">Accepted for interface compatibility; fallback parents are controlled by this localizer.</param>
    /// <returns>The merged localized resource values.</returns>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var culture in GetFallbackCultures(CultureInfo.CurrentUICulture))
        {
            var resourceSet = GetResourceSet(culture);
            if (resourceSet is null)
            {
                continue;
            }

            foreach (System.Collections.DictionaryEntry entry in resourceSet)
            {
                if (entry.Key is string key && entry.Value is string value)
                {
                    values.TryAdd(key, value);
                }
            }
        }

        return values
            .Select(item => new LocalizedString(
                item.Key,
                item.Value,
                resourceNotFound: false,
                searchedLocation: _searchedLocation))
            .ToArray();
    }

    /// <summary>
    /// Resolves one resource key by trying the current UI culture, language fallback,
    /// English, Chinese, and finally invariant resources.
    /// </summary>
    /// <param name="name">The resource key to resolve.</param>
    /// <param name="arguments">Reserved for the indexed localizer path; resolution itself does not format values.</param>
    /// <returns>The first matching localized value, or a resource-not-found result.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, empty, or whitespace.</exception>
    private LocalizedString Resolve(string name, object[]? arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        foreach (var culture in GetFallbackCultures(CultureInfo.CurrentUICulture))
        {
            var resourceSet = GetResourceSet(culture);
            var value = resourceSet?.GetString(name, ignoreCase: false);
            if (value is null)
            {
                continue;
            }

            return new LocalizedString(name, value, resourceNotFound: false, searchedLocation: _searchedLocation);
        }

        return new LocalizedString(name, name, resourceNotFound: true, _searchedLocation);
    }

    /// <summary>
    /// Loads one resource set without allowing the resource manager to search parent cultures.
    /// Missing manifests are treated as an unavailable culture rather than propagated.
    /// </summary>
    /// <param name="culture">The culture whose resource set should be loaded.</param>
    /// <returns>The resource set, or <see langword="null"/> when it is unavailable.</returns>
    private ResourceSet? GetResourceSet(CultureInfo culture)
    {
        try
        {
            return _resourceManager.GetResourceSet(
                culture,
                createIfNotExists: true,
                tryParents: false);
        }
        catch (MissingManifestResourceException)
        {
            return null;
        }
    }

    /// <summary>
    /// Produces the ordered cultures used by the localizer's fallback policy.
    /// </summary>
    /// <param name="requestedCulture">The culture selected for the current UI request.</param>
    /// <returns>A duplicate-free sequence ordered from most specific to final fallback.</returns>
    private static IEnumerable<CultureInfo> GetFallbackCultures(CultureInfo requestedCulture)
    {
        var cultures = new List<CultureInfo>();
        var language = requestedCulture.TwoLetterISOLanguageName;

        Add(cultures, requestedCulture);

        if (language is not "zh" and not "en")
        {
            Add(cultures, CultureInfo.GetCultureInfo(language));
            Add(cultures, CultureInfo.GetCultureInfo("en"));
            Add(cultures, CultureInfo.GetCultureInfo("zh-CN"));
        }
        else if (language == "en")
        {
            Add(cultures, CultureInfo.GetCultureInfo("en"));
            Add(cultures, CultureInfo.GetCultureInfo("zh-CN"));
        }
        else
        {
            Add(cultures, CultureInfo.GetCultureInfo("zh-CN"));
        }

        Add(cultures, CultureInfo.InvariantCulture);
        return cultures;
    }

    /// <summary>
    /// Adds a culture when a culture with the same name is not already present.
    /// </summary>
    /// <param name="cultures">The collection receiving the culture.</param>
    /// <param name="culture">The culture to add.</param>
    private static void Add(ICollection<CultureInfo> cultures, CultureInfo culture)
    {
        if (cultures.All(item => !string.Equals(item.Name, culture.Name, StringComparison.OrdinalIgnoreCase)))
        {
            cultures.Add(culture);
        }
    }
}

/// <summary>
/// Resource manager factory used by NCF hosts so all IStringLocalizer consumers
/// share the same current-language, English, Chinese fallback chain.
/// </summary>
public sealed class NcfFallbackStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly ConcurrentDictionary<string, IStringLocalizer> _localizers = new(StringComparer.Ordinal);

    /// <summary>
    /// Creates or retrieves a cached localizer for a resource type.
    /// </summary>
    /// <param name="resourceSource">The resource type whose assembly and full name identify the resource set.</param>
    /// <returns>A cached localizer backed by the resource type's assembly.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourceSource"/> is <see langword="null"/>.</exception>
    public IStringLocalizer Create(Type resourceSource)
    {
        ArgumentNullException.ThrowIfNull(resourceSource);

        var key = $"{resourceSource.Assembly.FullName}|{resourceSource.FullName}";
        return _localizers.GetOrAdd(
            key,
            _ => new NcfFallbackStringLocalizer(
                new ResourceManager(resourceSource.FullName!, resourceSource.Assembly),
                resourceSource.Assembly.FullName ?? resourceSource.Assembly.GetName().Name ?? resourceSource.FullName!));
    }

    /// <summary>
    /// Creates or retrieves a cached localizer for an explicit resource base name and assembly location.
    /// </summary>
    /// <param name="baseName">The base name passed to <see cref="ResourceManager"/>.</param>
    /// <param name="location">The assembly name that contains the resource.</param>
    /// <returns>A cached localizer for the requested resource.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="baseName"/> or <paramref name="location"/> is empty or whitespace.</exception>
    public IStringLocalizer Create(string baseName, string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        var key = $"{location}|{baseName}";
        return _localizers.GetOrAdd(
            key,
            _ =>
            {
                var assembly = Assembly.Load(new AssemblyName(location));
                return new NcfFallbackStringLocalizer(
                    new ResourceManager(baseName, assembly),
                    location);
            });
    }
}
