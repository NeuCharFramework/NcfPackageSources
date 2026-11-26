/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfBuilderResource.cs
    File description: XncfBuilderResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.37.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.XncfBuilder
{
    /// <summary>
    /// Localization catalog owned and packaged by the XncfBuilder module.
    /// </summary>
    public sealed class XncfBuilderResource
    {
        /// <summary>
        /// Resolves an XncfBuilder localization key and returns the fallback when unavailable.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback text.</param>
        /// <returns>The localized text or fallback text.</returns>
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(XncfBuilderResource), key, fallback);
        }

        /// <summary>
        /// Resolves and formats an XncfBuilder localization template.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback composite format string.</param>
        /// <param name="arguments">The values used by the format template.</param>
        /// <returns>The localized formatted text.</returns>
        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(XncfBuilderResource), key, fallback, arguments);
        }
    }
}
