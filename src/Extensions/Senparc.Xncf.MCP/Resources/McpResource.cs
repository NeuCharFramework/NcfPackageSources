/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: McpResource.cs
    File description: McpResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.4.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.5.14 Updated module behavior, localization metadata, and English source comments

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
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(McpResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(McpResource), key, fallback, arguments);
        }
    }
}
