using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.Dapr.Utils.Serializer;

namespace Senparc.Xncf.Dapr;

public static class DaprClientServiceCollectionExtensions
{
    /// <summary>
    /// Register the Dapr client to the service
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configOptions">DaprClient configuration options</param>
    public static void AddDaprClient(this IServiceCollection services, Action<DaprClientOptions>? configOptions = null)
    {
        AddDaprClient<Serializer>(services, configOptions);
    }

    /// <summary>
    /// Register the Dapr client to the service
    /// </summary>
    /// <typeparam name="TSerializer">Implementation type of serializer interface</typeparam>
    /// <param name="services"></param>
    /// <param name="configOptions">DaprClient configuration options</param>
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