/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SystemPermissionResource.cs
    File description: SystemPermissionResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.15.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.15.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.SystemPermission
{
    /// <summary>
    /// Localization catalog owned and packaged by the SystemPermission module.
    /// </summary>
    public sealed class SystemPermissionResource
    {
        /// <summary>
        /// Resolves a SystemPermission localization key and returns the fallback when unavailable.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback text.</param>
        /// <returns>The localized text or fallback text.</returns>
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(SystemPermissionResource), key, fallback);
        }

        /// <summary>
        /// Resolves and formats a SystemPermission localization template.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback composite format string.</param>
        /// <param name="arguments">The values used by the format template.</param>
        /// <returns>The localized formatted text.</returns>
        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(SystemPermissionResource), key, fallback, arguments);
        }
    }
}
