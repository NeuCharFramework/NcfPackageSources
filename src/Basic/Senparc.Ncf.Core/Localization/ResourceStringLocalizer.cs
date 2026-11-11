/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ResourceStringLocalizer.cs
    文件功能描述：ResourceStringLocalizer 相关实现
    
    
    创建标识：Senparc - 20260717
    
    修改标识：Senparc - 20260717
    修改描述：v0.25.0-preview2 新增统一资源本地化基础设施并支持 FunctionRender 动态文案

    修改标识：Senparc - 20261111
    修改描述：v0.30.4-preview10 统一本地化资源回退与语言配置能力

----------------------------------------------------------------*/
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Resources;

namespace Senparc.Ncf.Core.Localization
{
    /// <summary>
    /// Resolves strongly associated .resx resources without requiring a DI scope.
    /// It is primarily used by attributes and XNCF registration metadata, where
    /// constructor injection is not available.
    /// </summary>
    public static class ResourceStringLocalizer
    {
        private static readonly ConcurrentDictionary<Type, NcfFallbackStringLocalizer> Localizers = new();

        public static string Get(
            Type resourceType,
            string resourceKey,
            string fallback = null,
            CultureInfo culture = null)
        {
            ArgumentNullException.ThrowIfNull(resourceType);
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceKey);

            var localizer = Localizers.GetOrAdd(
                resourceType,
                type => new NcfFallbackStringLocalizer(
                    new ResourceManager(type.FullName!, type.Assembly),
                    type.Assembly.FullName ?? type.Assembly.GetName().Name ?? type.FullName!));

            var originalCulture = CultureInfo.CurrentUICulture;
            try
            {
                if (culture is not null)
                {
                    CultureInfo.CurrentUICulture = culture;
                }

                var localized = localizer[resourceKey];
                return localized.ResourceNotFound
                    ? fallback ?? resourceKey
                    : localized.Value;
            }
            finally
            {
                if (culture is not null)
                {
                    CultureInfo.CurrentUICulture = originalCulture;
                }
            }
        }

        public static string Format(
            Type resourceType,
            string resourceKey,
            string fallback,
            params object[] arguments)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                Get(resourceType, resourceKey, fallback),
                arguments ?? Array.Empty<object>());
        }
    }
}
