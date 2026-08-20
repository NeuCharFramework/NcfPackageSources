/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DaprClientServiceCollectionExtensions.cs
    文件功能描述：DaprClientServiceCollectionExtensions 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
        services.Configure<DaprClientOptions>(configOptions);

        services.AddHttpClient<DaprClient>();
        services.AddScoped<ISerializer, TSerializer>();
    }
}