/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIKernelResource.cs
    File description: AIKernelResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.14.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.AIKernel
{
    /// <summary>
    /// Localization catalog owned and packaged by the AIKernel module.
    /// </summary>
    public sealed class AIKernelResource
    {
        /// <summary>
        /// Resolves an AIKernel localization key and returns the fallback when unavailable.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback text.</param>
        /// <returns>The localized text or fallback text.</returns>
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(AIKernelResource), key, fallback);
        }

        /// <summary>
        /// Resolves and formats an AIKernel localization template.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback composite format string.</param>
        /// <param name="arguments">The values used by the format template.</param>
        /// <returns>The localized formatted text.</returns>
        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(AIKernelResource), key, fallback, arguments);
        }
    }
}
