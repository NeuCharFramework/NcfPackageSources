using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Linq;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Ncf.Core.EventBus
{
    public static class EventBusExtensions
    {
        /// <summary>
        /// 注册 Senparc EventBus 及所有事件处理器
        /// </summary>
        public static IServiceCollection AddSenparcEventBus(this IServiceCollection services, params Assembly[] assembliesToScan)
        {
            // 1. 注册单例 EventBus (发布者用)
            services.AddSingleton<InMemoryEventBus>();
            services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<InMemoryEventBus>());

            // 2. 注册后台托管服务 (消费者用)
            services.AddHostedService<EventBusHostedService>();

            // 3. 扫描并注册 Handler
            if (assembliesToScan != null && assembliesToScan.Length > 0)
            {
                foreach (var assembly in assembliesToScan)
                {
                    // 查找实现了 IIntegrationEventHandler<T> 的具体类
                    var handlerTypes = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract &&
                               t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)));

                    foreach (var type in handlerTypes)
                    {
                        var interfaces = type.GetInterfaces()
                            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>));

                        foreach (var i in interfaces)
                        {
                            // 注册为 Scoped
                            services.AddScoped(i, type);
                        }
                    }
                }
            }

            return services;
        }
    }
}