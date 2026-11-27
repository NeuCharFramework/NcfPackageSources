/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxResource.cs
    File description: SandboxResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v1.1.0 update feature parameters and page localization for the sample XNCF module

    Modified by: Senparc - 20260724
    Change description: v1.1.0 improve multilingual support for XNCF template pages and resources

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.4.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Sandbox
{
    /// <summary>
    /// Localization catalog owned and packaged by the TemplateSimulated module.
    /// </summary>
    public sealed class SandboxResource
    {
        /// <summary>
        /// Resolves a Sandbox localization key and returns the fallback when unavailable.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback text.</param>
        /// <returns>The localized text or fallback text.</returns>
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(SandboxResource), key, fallback);
        }

        /// <summary>
        /// Resolves and formats a Sandbox localization template.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback composite format string.</param>
        /// <param name="arguments">The values used by the format template.</param>
        /// <returns>The localized formatted text.</returns>
        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(SandboxResource), key, fallback, arguments);
        }
    }
}
