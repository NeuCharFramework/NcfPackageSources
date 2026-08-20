/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SystemPermissionResource.cs
    文件功能描述：SystemPermissionResource 相关实现
    
    
    创建标识：Senparc - 20260717
    
    修改标识：Senparc - 20260717
    修改描述：v0.15.0-preview2 为 SystemPermission 模块接入统一资源本地化并优化功能文案

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.SystemPermission
{
    /// <summary>
    /// Localization catalog owned and packaged by the SystemPermission module.
    /// </summary>
    public sealed class SystemPermissionResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(SystemPermissionResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(SystemPermissionResource), key, fallback, arguments);
        }
    }
}
