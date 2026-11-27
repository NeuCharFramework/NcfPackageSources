/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ApplicationResource.cs
    File description: ApplicationResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.1.0 integrate shared resource localization and improve feature text for the Application module

    Modified by: Senparc - 20261119
    Change description: v0.1.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.1.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Application
{
    /// <summary>
    /// Localization catalog owned and packaged by the Application module.
    /// </summary>
    public sealed class ApplicationResource
    {
        /// <summary>
        /// Resolves an Application localization key and returns the fallback when unavailable.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback text.</param>
        /// <returns>The localized text or fallback text.</returns>
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(ApplicationResource), key, fallback);
        }

        /// <summary>
        /// Resolves and formats an Application localization template.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback composite format string.</param>
        /// <param name="arguments">The values used by the format template.</param>
        /// <returns>The localized formatted text.</returns>
        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(ApplicationResource), key, fallback, arguments);
        }
    }
}
