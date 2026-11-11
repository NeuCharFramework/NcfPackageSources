/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NcfLocalizationServiceCollectionExtensions.cs
    文件功能描述：模块功能与本地化支持
    
    创建标识：Senparc - 20261110
    
    修改标识：Senparc - 20261111
    修改描述：v0.30.4-preview10 统一本地化资源回退与语言配置能力

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Senparc.Ncf.Core.Localization;

public static class NcfLocalizationServiceCollectionExtensions
{
    /// <summary>
    /// Registers ASP.NET Core localization with NCF's current-language,
    /// English, Chinese fallback behavior.
    /// </summary>
    public static IServiceCollection AddNcfLocalization(this IServiceCollection services)
    {
        services.AddLocalization();
        services.AddSingleton<IStringLocalizerFactory, NcfFallbackStringLocalizerFactory>();
        return services;
    }
}
