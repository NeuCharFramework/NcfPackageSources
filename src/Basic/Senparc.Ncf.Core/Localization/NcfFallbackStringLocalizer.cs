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

    public NcfFallbackStringLocalizer(ResourceManager resourceManager, string searchedLocation)
    {
        _resourceManager = resourceManager;
        _searchedLocation = searchedLocation;
    }

    public LocalizedString this[string name] =>
        Resolve(name, null);

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
