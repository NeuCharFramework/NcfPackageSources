using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.Dapr.Utils.Serializer;

namespace Senparc.Xncf.Dapr;

public static class DaprClientServiceCollectionExtensions
{
    /// <summary>
    /// 将Dapr客户端注册到服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configOptions">Dapr客户端配置选项</param>
    public static void AddDaprClient(this IServiceCollection services, Action<DaprClientOptions>? configOptions = null)
    {
        AddDaprClient<Serializer>(services, configOptions);
    }

    /// <summary>
    /// 将Dapr客户端注册到服务
    /// </summary>
    /// <typeparam name="TSerializer">序列化器接口的实现类型</typeparam>
    /// <param name="services"></param>
    /// <param name="configOptions">Dapr客户端配置选项</param>
    public static void AddDaprClient<TSerializer>(this IServiceCollection services, Action<DaprClientOptions>? configOptions = null) 
        where TSerializer : class, ISerializer
    {
        configOptions ??= options =>
        {
            options.ApiPort = 3500;
            options.DaprConnectionRetryCount = 3;
        };
        services.Configure(configOptions);

        services.AddHttpClient<DaprClient>();
        services.AddScoped<ISerializer, TSerializer>();
    }
}