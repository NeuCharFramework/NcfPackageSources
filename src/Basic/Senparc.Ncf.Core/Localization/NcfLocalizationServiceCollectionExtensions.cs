/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: NcfLocalizationServiceCollectionExtensions.cs
    File description: Related implementation.
    
    Created by: Senparc - 20261110
    
    Modified by: Senparc - 20261111
    Change description: v0.30.4-preview10 Updated module behavior and related support.

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
