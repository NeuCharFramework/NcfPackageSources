/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DynamicDataResource.cs
    文件功能描述：DynamicDataResource 相关实现
    
    
    创建标识：Senparc - 20260717
    
    修改标识：Senparc - 20260717
    修改描述：v0.6.0-preview2 为 DynamicData 模块接入统一资源本地化并优化功能文案

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.DynamicData
{
    /// <summary>
    /// Localization catalog owned and packaged by the DynamicData module.
    /// </summary>
    public sealed class DynamicDataResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(DynamicDataResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(DynamicDataResource), key, fallback, arguments);
        }
    }
}
