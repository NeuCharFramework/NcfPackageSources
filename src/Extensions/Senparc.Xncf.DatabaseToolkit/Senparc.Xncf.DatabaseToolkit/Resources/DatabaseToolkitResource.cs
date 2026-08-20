/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DatabaseToolkitResource.cs
    文件功能描述：DatabaseToolkitResource 相关实现
    
    
    创建标识：Senparc - 20260717
    
    修改标识：Senparc - 20260717
    修改描述：v0.26.0-preview2 为 DatabaseToolkit 模块接入统一资源本地化并优化功能文案

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.DatabaseToolkit
{
    /// <summary>
    /// Localization catalog owned and packaged by the DatabaseToolkit module.
    /// </summary>
    public sealed class DatabaseToolkitResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(DatabaseToolkitResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(DatabaseToolkitResource), key, fallback, arguments);
        }
    }
}
