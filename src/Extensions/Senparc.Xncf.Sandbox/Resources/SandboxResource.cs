/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SandboxResource.cs
    文件功能描述：SandboxResource 相关实现
    
    
    创建标识：Senparc - 20260717
    
    修改标识：Senparc - 20260717
    修改描述：v1.1.0 更新示例 XNCF 模块的功能参数与页面本地化能力

    修改标识：Senparc - 20260724
    修改描述：v1.1.0 完善 XNCF 模板页面与资源的多语言支持

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Sandbox
{
    /// <summary>
    /// Localization catalog owned and packaged by the TemplateSimulated module.
    /// </summary>
    public sealed class SandboxResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(SandboxResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(SandboxResource), key, fallback, arguments);
        }
    }
}
