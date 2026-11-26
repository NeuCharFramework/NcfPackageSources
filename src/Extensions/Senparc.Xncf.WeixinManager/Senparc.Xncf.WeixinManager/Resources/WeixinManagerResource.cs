/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinManagerResource.cs
    File description: WeixinManagerResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.23.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.WeixinManager
{
    /// <summary>
    /// Localization catalog owned and packaged by the WeixinManager module.
    /// </summary>
    public sealed class WeixinManagerResource
    {
        /// <summary>
        /// Resolves a WeixinManager localization key and returns the fallback when unavailable.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback text.</param>
        /// <returns>The localized text or fallback text.</returns>
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(WeixinManagerResource), key, fallback);
        }

        /// <summary>
        /// Resolves and formats a WeixinManager localization template.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback composite format string.</param>
        /// <param name="arguments">The values used by the format template.</param>
        /// <returns>The localized formatted text.</returns>
        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(WeixinManagerResource), key, fallback, arguments);
        }
    }
}
