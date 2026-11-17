using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Localization;
using Senparc.Ncf.Core;

namespace Senparc.Ncf.Core.Localization;

/// <summary>
/// Resolves legacy direct FunctionRender text through the shared frontend catalog.
/// This keeps existing attributes source-compatible while allowing their display
/// names and descriptions to follow the current UI culture.
/// </summary>
public static class FunctionRenderTextResolver
{
    private static readonly IStringLocalizerFactory Factory = new NcfFallbackStringLocalizerFactory();
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<Type>> ResourceTypes = new();

    public static string Resolve(Type registerType, string source)
    {
        if (string.IsNullOrWhiteSpace(source) || registerType == null)
        {
            return source;
        }

        foreach (var resourceType in GetResourceTypes(registerType))
        {
            var translations = FrontendTranslationCatalog.Get(Factory, resourceType);
            if (translations.TryGetValue(source, out var localized)
                && !string.Equals(localized, source, StringComparison.Ordinal))
            {
                return localized;
            }
        }

        return source;
    }

    private static IReadOnlyList<Type> GetResourceTypes(Type registerType)
    {
        return ResourceTypes.GetOrAdd(registerType, type =>
        {
            var result = new List<Type> { typeof(NcfCoreResource) };
            result.AddRange(type.Assembly
                .GetTypes()
                .Where(candidate => candidate.Name.EndsWith("Resource", StringComparison.Ordinal)
                    && candidate != typeof(NcfCoreResource)));
            return result.Distinct().ToArray();
        });
    }
}
