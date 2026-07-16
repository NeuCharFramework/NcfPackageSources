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

        public override string Description =>
            ResourceStringLocalizer.Get(ResourceType, ResourceKey, base.Description);
    }
}
