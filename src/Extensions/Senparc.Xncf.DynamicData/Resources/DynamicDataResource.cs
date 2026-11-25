/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DynamicDataResource.cs
    File description: DynamicDataResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.6.0-preview2 integrate shared resource localization and improve feature text for the DynamicData module

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.DynamicData
{
    /// <summary>
    /// Localization catalog owned and packaged by the DynamicData module.
    /// </summary>
    public sealed class DynamicDataResource
    {
        /// <summary>
        /// Resolves a localized resource value with an optional fallback.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="fallback">The value returned when the key is unavailable.</param>
        /// <returns>The localized or fallback text.</returns>
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(DynamicDataResource), key, fallback);
        }

        /// <summary>
        /// Resolves and formats a localized resource value with the supplied arguments.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="fallback">The fallback format string.</param>
        /// <param name="arguments">The format arguments.</param>
        /// <returns>The localized formatted text.</returns>
        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(DynamicDataResource), key, fallback, arguments);
        }
    }
}
