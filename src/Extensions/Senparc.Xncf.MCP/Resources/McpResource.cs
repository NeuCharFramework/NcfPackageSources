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
