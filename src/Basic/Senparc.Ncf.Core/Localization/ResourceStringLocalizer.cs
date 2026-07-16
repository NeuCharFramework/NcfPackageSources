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
        private static readonly ConcurrentDictionary<Type, ResourceManager> ResourceManagers = new();

        public static string Get(
            Type resourceType,
            string resourceKey,
            string fallback = null,
            CultureInfo culture = null)
        {
            ArgumentNullException.ThrowIfNull(resourceType);
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceKey);

            var resourceManager = ResourceManagers.GetOrAdd(
                resourceType,
                type => new ResourceManager(type.FullName!, type.Assembly));

            return resourceManager.GetString(resourceKey, culture ?? CultureInfo.CurrentUICulture)
                   ?? fallback
                   ?? resourceKey;
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
