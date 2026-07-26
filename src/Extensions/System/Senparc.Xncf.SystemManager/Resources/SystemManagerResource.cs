/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SystemManagerResource.cs
    文件功能描述：SystemManagerResource 相关实现
    
    
    创建标识：Senparc - 20260717
    
    修改标识：Senparc - 20260717
    修改描述：v0.15.0-preview3 为 SystemManager 模块接入统一资源本地化并优化功能文案

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.SystemManager
{
    /// <summary>
    /// Localization catalog owned and packaged by the SystemManager module.
    /// </summary>
    public sealed class SystemManagerResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(SystemManagerResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(SystemManagerResource), key, fallback, arguments);
        }
    }
}
