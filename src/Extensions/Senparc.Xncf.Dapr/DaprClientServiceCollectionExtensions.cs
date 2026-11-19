/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DaprClientServiceCollectionExtensions.cs
    File description: DaprClientServiceCollectionExtensions related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.11.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.Dapr.Utils.Serializer;

namespace Senparc.Xncf.Dapr;

public static class DaprClientServiceCollectionExtensions
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    /// <param name="services"></param>
    ///Legacy comment translated to English.
    public static void AddDaprClient(this IServiceCollection services, Action<DaprClientOptions>? configOptions = null)
    {
        AddDaprClient<Serializer>(services, configOptions);
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    ///Legacy comment translated to English.
    /// <param name="services"></param>
    ///Legacy comment translated to English.
    public static void AddDaprClient<TSerializer>(this IServiceCollection services, Action<DaprClientOptions>? configOptions = null) 
        where TSerializer : class, ISerializer
    {
        configOptions ??= options =>
        {
            options.ApiPort = 3500;
            options.DaprConnectionRetryCount = 3;
        };
        services.Configure<DaprClientOptions>(configOptions);

        services.AddHttpClient<DaprClient>();
        services.AddScoped<ISerializer, TSerializer>();
    }
}