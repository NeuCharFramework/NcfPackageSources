/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WeixinManagerResource.cs
    文件功能描述：WeixinManagerResource 相关实现
    
    
    创建标识：Senparc - 20260717
    
    修改标识：Senparc - 20260717
    修改描述：v0.23.0-preview3 为 WeixinManager 模块接入统一资源本地化并优化功能文案

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.WeixinManager
{
    /// <summary>
    /// Localization catalog owned and packaged by the WeixinManager module.
    /// </summary>
    public sealed class WeixinManagerResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(WeixinManagerResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(WeixinManagerResource), key, fallback, arguments);
        }
    }
}
