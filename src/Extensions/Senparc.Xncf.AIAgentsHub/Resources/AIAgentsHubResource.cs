/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AIAgentsHubResource.cs
    文件功能描述：AIAgentsHubResource 相关实现
    
    
    创建标识：Senparc - 20260717
    
    修改标识：Senparc - 20260717
    修改描述：v0.13.0-preview3 为 AIAgentsHub 模块接入统一资源本地化并优化功能文案

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.AIAgentsHub
{
    /// <summary>
    /// Localization catalog owned and packaged by the AIAgentsHub module.
    /// </summary>
    public sealed class AIAgentsHubResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(AIAgentsHubResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(AIAgentsHubResource), key, fallback, arguments);
        }
    }
}
