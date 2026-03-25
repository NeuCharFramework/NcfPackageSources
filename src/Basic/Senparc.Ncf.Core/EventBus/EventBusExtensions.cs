using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Linq;
using Senparc.Ncf.Shared.Abstractions.Events;
using System;

namespace Senparc.Ncf.Core.EventBus
{
    public static class EventBusExtensions
    {
        /// <summary>
        /// 注册 Senparc EventBus 及所有事件处理器
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configureOptions">配置 EventBus 选项的委托（可选）</param>
        /// <param name="assembliesToScan">需要扫描事件处理器的程序集</param>
        public static IServiceCollection AddSenparcEventBus(
            this IServiceCollection services, 
            Action<EventBusOptions> configureOptions = null,
            params Assembly[] assembliesToScan)
        {
            // 1. 注册 EventBus 配置选项
            var options = new EventBusOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(options);

            // 2. 注册单例 EventBus (发布者用) - 使用工厂方法来支持 ILogger 注入
            services.AddSingleton<InMemoryEventBus>(sp =>
            {
                var logger = sp.GetService<ILogger<InMemoryEventBus>>();
                return new InMemoryEventBus(logger);
            });
            services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<InMemoryEventBus>());

            // 3. 注册后台托管服务 (消费者用)
            services.AddHostedService<EventBusHostedService>();

            // 4. 扫描并注册 Handler
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