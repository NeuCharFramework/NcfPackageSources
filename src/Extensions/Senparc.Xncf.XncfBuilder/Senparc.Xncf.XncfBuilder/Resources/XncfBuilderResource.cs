/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XncfBuilderResource.cs
    文件功能描述：XncfBuilderResource 相关实现
    
    
    创建标识：Senparc - 20260717
    
    修改标识：Senparc - 20260717
    修改描述：v0.37.0-preview5 增强 XNCF 构建、数据库迁移与 AI 生成流程的本地化支持

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.XncfBuilder
{
    /// <summary>
    /// Localization catalog owned and packaged by the XncfBuilder module.
    /// </summary>
    public sealed class XncfBuilderResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(XncfBuilderResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(XncfBuilderResource), key, fallback, arguments);
        }
    }
}
