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

        /// <summary>
        /// Resolves a localized resource value for the requested resource type and key.
        /// A supplied culture is applied temporarily and restored before returning.
        /// </summary>
        /// <param name="resourceType">The resource class whose assembly contains the resource.</param>
        /// <param name="resourceKey">The resource key to resolve.</param>
        /// <param name="fallback">The value returned when the key is not found.</param>
        /// <param name="culture">Optional UI culture used for this lookup only.</param>
        /// <returns>The localized value, fallback value, or resource key.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourceType"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="resourceKey"/> is empty.</exception>
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

        /// <summary>
        /// Resolves a localized resource value and formats it with the current culture.
        /// </summary>
        /// <param name="resourceType">The resource class whose assembly contains the resource.</param>
        /// <param name="resourceKey">The resource key to resolve.</param>
        /// <param name="fallback">The fallback format string when the key is missing.</param>
        /// <param name="arguments">The values supplied to <see cref="string.Format(IFormatProvider, string, object[])"/>.</param>
        /// <returns>The formatted localized string.</returns>
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
