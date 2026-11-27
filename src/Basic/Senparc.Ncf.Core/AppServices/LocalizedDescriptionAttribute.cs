/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: LocalizedDescriptionAttribute.cs
    File description: LocalizedDescriptionAttribute related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.25.0-preview2 add unify resource localization base facilities and support FunctionRender dynamic text

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/
using Senparc.Ncf.Core.Localization;
using System;
using System.ComponentModel;

namespace Senparc.Ncf.Core.AppServices
{
    /// <summary>
    /// A <see cref="DescriptionAttribute"/> whose value is resolved from a .resx
    /// resource for the current request culture.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Method,
        AllowMultiple = false,
        Inherited = true)]
    public sealed class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        public Type ResourceType { get; }

        public string ResourceKey { get; }

        public string HelpResourceKey { get; }

        /// <summary>
        /// Initializes a localized description attribute.
        /// </summary>
        /// <param name="resourceType">The resource type containing the description.</param>
        /// <param name="resourceKey">The description resource key.</param>
        /// <param name="fallback">Fallback description text.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourceType"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="resourceKey"/> is blank.</exception>
        public LocalizedDescriptionAttribute(
            Type resourceType,
            string resourceKey,
            string fallback = null)
            : base(fallback ?? resourceKey)
        {
            ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
            ResourceKey = string.IsNullOrWhiteSpace(resourceKey)
                ? throw new ArgumentException("A resource key is required.", nameof(resourceKey))
                : resourceKey;
        }

        /// <summary>
        /// Initializes a localized description with a separate help-text resource key.
        /// </summary>
        /// <param name="resourceType">The resource type containing the description.</param>
        /// <param name="resourceKey">The description resource key.</param>
        /// <param name="helpResourceKey">The help-text resource key.</param>
        /// <param name="fallback">Fallback description text.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="helpResourceKey"/> is blank.</exception>
        public LocalizedDescriptionAttribute(
            Type resourceType,
            string resourceKey,
            string helpResourceKey,
            string fallback = null)
            : this(resourceType, resourceKey, fallback)
        {
            HelpResourceKey = string.IsNullOrWhiteSpace(helpResourceKey)
                ? throw new ArgumentException("A help resource key is required.", nameof(helpResourceKey))
                : helpResourceKey;
        }

        /// <summary>
        /// Resolves the description and optional help text for the current culture.
        /// </summary>
        public override string Description =>
            string.IsNullOrWhiteSpace(HelpResourceKey)
                ? ResourceStringLocalizer.Get(ResourceType, ResourceKey, base.Description)
                : $"{ResourceStringLocalizer.Get(ResourceType, ResourceKey, base.Description)}||" +
                  ResourceStringLocalizer.Get(ResourceType, HelpResourceKey, string.Empty);
    }
}
