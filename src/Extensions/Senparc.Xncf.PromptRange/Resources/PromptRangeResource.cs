/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptRangeResource.cs
    File description: PromptRangeResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.17.0-preview5 integrate shared resource localization and improve feature text for the PromptRange module

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.PromptRange
{
    /// <summary>
    /// Localization catalog owned and packaged by the PromptRange module.
    /// </summary>
    public sealed class PromptRangeResource
    {
        /// <summary>
        /// Resolves a localized PromptRange resource value.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="fallback">The fallback value used when the key is unavailable.</param>
        /// <returns>The localized or fallback value.</returns>
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(PromptRangeResource), key, fallback);
        }

        /// <summary>
        /// Resolves and formats a localized PromptRange resource value.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="fallback">The fallback format string.</param>
        /// <param name="arguments">The values substituted into the format string.</param>
        /// <returns>The formatted localized or fallback value.</returns>
        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(PromptRangeResource), key, fallback, arguments);
        }
    }
}
