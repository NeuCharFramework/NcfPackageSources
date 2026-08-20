/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptRangeResource.cs
    文件功能描述：PromptRangeResource 相关实现
    
    
    创建标识：Senparc - 20260717
    
    修改标识：Senparc - 20260717
    修改描述：v0.17.0-preview5 为 PromptRange 模块接入统一资源本地化并优化功能文案

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.PromptRange
{
    /// <summary>
    /// Localization catalog owned and packaged by the PromptRange module.
    /// </summary>
    public sealed class PromptRangeResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(PromptRangeResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(PromptRangeResource), key, fallback, arguments);
        }
    }
}
