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
