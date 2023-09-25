using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.Dapr.Utils.Serializer;

namespace Senparc.Xncf.Dapr;

public static class DaprClientServiceCollectionExtensions
{
    public static void AddDaprClient(this IServiceCollection services)
    {
        services.AddHttpClient<DaprClient>();
        services.AddScoped<ISerializer, Serializer>();
    }
    public static void AddDaprClient<TSerializer>(this IServiceCollection services) where TSerializer : class, ISerializer
    {
        services.AddHttpClient<DaprClient>();
        services.AddScoped<ISerializer, TSerializer>();
    }
}