/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：McpResource.cs
    文件功能描述：McpResource 相关实现
    
    
    创建标识：Senparc - 20260717
    
    修改标识：Senparc - 20260717
    修改描述：v0.4.0-preview3 为 MCP 模块接入统一资源本地化并优化功能文案

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
