/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TerminalResource.cs
    File description: TerminalResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.22.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.22.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.22.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Terminal
{
    /// <summary>
    /// Localization catalog owned and packaged by the Terminal module.
    /// </summary>
    public sealed class TerminalResource
    {
        /// <summary>
        /// Resolves a Terminal localization key and returns the fallback when the key is unavailable.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The text returned when the key has no localized value.</param>
        /// <returns>The localized text or the fallback text.</returns>
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(TerminalResource), key, fallback);
        }

        /// <summary>
        /// Resolves a Terminal localization key and formats the localized or fallback template.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback composite format string.</param>
        /// <param name="arguments">The values supplied to the format template.</param>
        /// <returns>The localized formatted text.</returns>
        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(TerminalResource), key, fallback, arguments);
        }
    }
}
