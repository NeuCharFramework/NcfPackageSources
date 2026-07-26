/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Template_XncfNameResource.cs
    文件功能描述：Template_XncfNameResource 相关实现
    
    
    创建标识：Senparc - 20260717
    
    修改标识：Senparc - 20260717
    修改描述：v1.1.0 更新示例 XNCF 模块的功能参数与页面本地化能力

    修改标识：Senparc - 20260724
    修改描述：v1.1.0 完善 XNCF 模板页面与资源的多语言支持

    修改标识：Senparc - 20260726
    修改描述：v1.1.0 补充示例模板 EventBus 请求-响应回环与多语言能力

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Template_OrgName.Xncf.Template_XncfName
{
    /// <summary>
    /// Localization catalog owned and packaged by the TemplateSimulated module.
    /// </summary>
    public sealed class Template_XncfNameResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(Template_XncfNameResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(Template_XncfNameResource), key, fallback, arguments);
        }
    }
}
