/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：LocalizedDescriptionAttribute.cs
    文件功能描述：LocalizedDescriptionAttribute 相关实现
    
    
    创建标识：Senparc - 20260717
    
    修改标识：Senparc - 20260717
    修改描述：v0.25.0-preview2 新增统一资源本地化基础设施并支持 FunctionRender 动态文案

----------------------------------------------------------------*/
using Senparc.Ncf.Core.Localization;
using System;
using System.ComponentModel;

namespace Senparc.Ncf.Core.AppServices
{
    /// <summary>
    /// A <see cref="DescriptionAttribute"/> whose value is resolved from a .resx
    /// resource for the current request culture.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Method,
        AllowMultiple = false,
        Inherited = true)]
    public sealed class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        public Type ResourceType { get; }

        public string ResourceKey { get; }

        public LocalizedDescriptionAttribute(
            Type resourceType,
            string resourceKey,
            string fallback = null)
            : base(fallback ?? resourceKey)
        {
            ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
            ResourceKey = string.IsNullOrWhiteSpace(resourceKey)
                ? throw new ArgumentException("A resource key is required.", nameof(resourceKey))
                : resourceKey;
        }

        public override string Description =>
            ResourceStringLocalizer.Get(ResourceType, ResourceKey, base.Description);
    }
}
