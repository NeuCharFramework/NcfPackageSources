/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: LocalizedRequiredAttribute.cs
    File description: LocalizedRequiredAttribute related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.25.0-preview2 add unify resource localization base facilities and support FunctionRender dynamic text

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/
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

        /// <summary>
        /// Initializes a culture-aware required-field validator.
        /// </summary>
        /// <param name="resourceType">The resource type containing the validation message.</param>
        /// <param name="resourceKey">The validation message key.</param>
        /// <param name="fallback">Fallback validation format.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourceType"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="resourceKey"/> is blank.</exception>
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

        /// <summary>
        /// Resolves the validation message in the current culture and formats it with the field name.
        /// </summary>
        /// <param name="name">The display name of the validated field.</param>
        /// <returns>The localized formatted validation message.</returns>
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
