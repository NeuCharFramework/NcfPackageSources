/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DaprResource.cs
    File description: DaprResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.11.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.11.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.11.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Dapr
{
    /// <summary>
    /// Localization catalog owned and packaged by the Dapr module.
    /// </summary>
    public sealed class DaprResource
    {
        /// <summary>
        /// Resolves a Dapr localization key and returns the fallback when the key is unavailable.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The text returned when the key has no localized value.</param>
        /// <returns>The localized text or the fallback text.</returns>
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(DaprResource), key, fallback);
        }

        /// <summary>
        /// Resolves a Dapr localization key and formats the localized or fallback template.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback composite format string.</param>
        /// <param name="arguments">The values supplied to the format template.</param>
        /// <returns>The localized formatted text.</returns>
        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(DaprResource), key, fallback, arguments);
        }
    }
}
