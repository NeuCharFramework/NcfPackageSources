/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ApplicationResource.cs
    文件功能描述：ApplicationResource 相关实现
    
    
    创建标识：Senparc - 20260717
    
    修改标识：Senparc - 20260717
    修改描述：v0.1.0 为 Application 模块接入统一资源本地化并优化功能文案

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Application
{
    /// <summary>
    /// Localization catalog owned and packaged by the Application module.
    /// </summary>
    public sealed class ApplicationResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(ApplicationResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(ApplicationResource), key, fallback, arguments);
        }
    }
}
