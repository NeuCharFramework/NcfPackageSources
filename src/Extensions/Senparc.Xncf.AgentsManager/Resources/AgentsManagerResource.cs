/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AgentsManagerResource.cs
    文件功能描述：AgentsManagerResource 相关实现
    
    
    创建标识：Senparc - 20260717
    
    修改标识：Senparc - 20260717
    修改描述：v0.12.0-preview6 为 AgentsManager 模块接入统一资源本地化并优化功能文案

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
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(AgentsManagerResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(AgentsManagerResource), key, fallback, arguments);
        }
    }
}
