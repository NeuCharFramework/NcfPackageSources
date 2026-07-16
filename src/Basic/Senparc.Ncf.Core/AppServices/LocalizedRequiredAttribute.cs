using Senparc.Ncf.Core.Localization;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Senparc.Ncf.Core.AppServices
{
    /// <summary>
    /// A required-field validator whose error message is resolved for the
    /// current request culture when validation runs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public sealed class LocalizedRequiredAttribute : RequiredAttribute
    {
        public Type ResourceType { get; }

        public string ResourceKey { get; }

        public string Fallback { get; }

        public LocalizedRequiredAttribute(
            Type resourceType,
            string resourceKey,
            string fallback = null)
        {
            ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
            ResourceKey = string.IsNullOrWhiteSpace(resourceKey)
                ? throw new ArgumentException("A resource key is required.", nameof(resourceKey))
                : resourceKey;
            Fallback = fallback;
        }

        public override string FormatErrorMessage(string name)
        {
            var format = ResourceStringLocalizer.Get(
                ResourceType,
                ResourceKey,
                Fallback ?? "The {0} field is required.");

            return string.Format(CultureInfo.CurrentCulture, format, name);
        }
    }
}
