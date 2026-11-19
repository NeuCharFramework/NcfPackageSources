/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ResourceStringLocalizer.cs
    File description: ResourceStringLocalizer related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.25.0-preview2 add unify resource localization base facilities and support FunctionRender dynamic text

    Modified by: Senparc - 20261111
    Change description: v0.30.4-preview10 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Resources;

namespace Senparc.Ncf.Core.Localization
{
    /// <summary>
    /// Resolves strongly associated .resx resources without requiring a DI scope.
    /// It is primarily used by attributes and XNCF registration metadata, where
    /// constructor injection is not available.
    /// </summary>
    public static class ResourceStringLocalizer
    {
        private static readonly ConcurrentDictionary<Type, NcfFallbackStringLocalizer> Localizers = new();

        public static string Get(
            Type resourceType,
            string resourceKey,
            string fallback = null,
            CultureInfo culture = null)
        {
            ArgumentNullException.ThrowIfNull(resourceType);
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceKey);

            var localizer = Localizers.GetOrAdd(
                resourceType,
                type => new NcfFallbackStringLocalizer(
                    new ResourceManager(type.FullName!, type.Assembly),
                    type.Assembly.FullName ?? type.Assembly.GetName().Name ?? type.FullName!));

            var originalCulture = CultureInfo.CurrentUICulture;
            try
            {
                if (culture is not null)
                {
                    CultureInfo.CurrentUICulture = culture;
                }

                var localized = localizer[resourceKey];
                return localized.ResourceNotFound
                    ? fallback ?? resourceKey
                    : localized.Value;
            }
            finally
            {
                if (culture is not null)
                {
                    CultureInfo.CurrentUICulture = originalCulture;
                }
            }
        }

        public static string Format(
            Type resourceType,
            string resourceKey,
            string fallback,
            params object[] arguments)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                Get(resourceType, resourceKey, fallback),
                arguments ?? Array.Empty<object>());
        }
    }
}
