/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentsManagerResource.cs
    File description: AgentsManagerResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.12.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.AgentsManager
{
    /// <summary>
    /// Localization catalog owned and packaged by the AgentsManager module.
    /// </summary>
    public sealed class AgentsManagerResource
    {
        /// <summary>
        /// Resolves an AgentsManager localization key and returns the fallback when unavailable.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback text.</param>
        /// <returns>The localized text or fallback text.</returns>
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(AgentsManagerResource), key, fallback);
        }

        /// <summary>
        /// Resolves and formats an AgentsManager localization template.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback composite format string.</param>
        /// <param name="arguments">The values used by the format template.</param>
        /// <returns>The localized formatted text.</returns>
        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(AgentsManagerResource), key, fallback, arguments);
        }
    }
}
