/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AIKernelResource.cs
    文件功能描述：AIKernelResource 相关实现
    
    
    创建标识：Senparc - 20260717
    
    修改标识：Senparc - 20260717
    修改描述：v0.14.0-preview5 为 AIKernel 模块接入统一资源本地化并优化功能文案

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.AIKernel
{
    /// <summary>
    /// Localization catalog owned and packaged by the AIKernel module.
    /// </summary>
    public sealed class AIKernelResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(AIKernelResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(AIKernelResource), key, fallback, arguments);
        }
    }
}
