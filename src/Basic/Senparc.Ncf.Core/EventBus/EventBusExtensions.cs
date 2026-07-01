/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：EventBusExtensions.cs
    文件功能描述：EventBusExtensions 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

----------------------------------------------------------------*/

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
            try
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

            }
            catch (Exception ex)
            {
                Senparc.CO2NET.Trace.SenparcTrace.SendCustomLog("AddSenparcEventBus 异常", ex.Message);
                Senparc.CO2NET.Trace.SenparcTrace.BaseExceptionLog(ex);
            }
                return services;
        }
    }
}