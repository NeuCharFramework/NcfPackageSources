/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: McpResource.cs
    File description: McpResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.4.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.5.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.5.14.2 Expand MCP endpoint gateway, credentials, and tool invocation support.

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.MCP
{
    /// <summary>
    /// Localization catalog owned and packaged by the MCP module.
    /// </summary>
    public sealed class McpResource
    {
        /// <summary>
        /// Resolves an MCP localization key and returns the fallback when unavailable.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback text.</param>
        /// <returns>The localized text or fallback text.</returns>
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(McpResource), key, fallback);
        }

        /// <summary>
        /// Resolves and formats an MCP localization template.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback composite format string.</param>
        /// <param name="arguments">The values used by the format template.</param>
        /// <returns>The localized formatted text.</returns>
        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(McpResource), key, fallback, arguments);
        }
    }
}
