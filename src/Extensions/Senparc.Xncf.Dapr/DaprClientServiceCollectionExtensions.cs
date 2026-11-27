/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DaprClientServiceCollectionExtensions.cs
    File description: DaprClientServiceCollectionExtensions related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.11.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.11.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.Dapr.Utils.Serializer;

namespace Senparc.Xncf.Dapr;

public static class DaprClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default Dapr client and JSON serializer.
    /// </summary>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="configOptions">Optional Dapr client configuration callback.</param>
    public static void AddDaprClient(this IServiceCollection services, Action<DaprClientOptions>? configOptions = null)
    {
        AddDaprClient<Serializer>(services, configOptions);
    }

    /// <summary>
    /// Registers the Dapr client with a caller-selected serializer and default API/retry settings.
    /// </summary>
    /// <typeparam name="TSerializer">The scoped serializer implementation.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="configOptions">Optional Dapr client configuration callback.</param>
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
