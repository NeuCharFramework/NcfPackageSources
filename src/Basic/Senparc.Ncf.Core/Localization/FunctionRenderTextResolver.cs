/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: FunctionRenderTextResolver.cs
    File description: FunctionRenderTextResolver related implementation

    Created by: Senparc - 20261117

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

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

    /// <summary>
    /// Resolves a FunctionRender source key against core and module resource catalogs.
    /// </summary>
    /// <param name="registerType">The module registration type whose assembly provides resources.</param>
    /// <param name="source">The source key or literal text.</param>
    /// <returns>The localized value when found; otherwise the original source.</returns>
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

    /// <summary>
    /// Discovers and caches resource types associated with an XNCF register assembly.
    /// </summary>
    /// <param name="registerType">The register type whose assembly is inspected.</param>
    /// <returns>The core resource type followed by discovered resource types.</returns>
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
