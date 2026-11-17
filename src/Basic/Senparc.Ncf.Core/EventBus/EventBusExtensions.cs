/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: EventBusExtensions.cs
    File description: EventBusExtensions related implementation
    
    
    Created by: Senparc - 20200724
    
    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260726
    Change description: v0.25.0-preview2 Updated module behavior and related support.

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
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        public static IServiceCollection AddSenparcEventBus(
            this IServiceCollection services, 
            Action<EventBusOptions> configureOptions = null,
            params Assembly[] assembliesToScan)
        {
            try
            {
                //Legacy comment translated to English.
                var options = new EventBusOptions();
                configureOptions?.Invoke(options);
                services.AddSingleton(options);

                //Legacy comment translated to English.
                services.AddSingleton<InMemoryEventBus>(sp =>
                {
                    var logger = sp.GetService<ILogger<InMemoryEventBus>>();
                    var eventBusOptions = sp.GetRequiredService<EventBusOptions>();
                    return new InMemoryEventBus(logger, eventBusOptions);
                });
                services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<InMemoryEventBus>());
                services.AddSingleton<IEventBusRequestClient>(sp => sp.GetRequiredService<InMemoryEventBus>());

                //Legacy comment translated to English.
                services.AddHostedService<EventBusHostedService>();

                //Legacy comment translated to English.
                if (assembliesToScan != null && assembliesToScan.Length > 0)
                {
                    foreach (var assembly in assembliesToScan)
                    {
                        //Legacy comment translated to English.
                        var handlerTypes = assembly.GetTypes()
                            .Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters &&
                                t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)));

                        foreach (var type in handlerTypes)
                        {
                            var interfaces = type.GetInterfaces()
                                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>));

                            foreach (var i in interfaces)
                            {
                                //Legacy comment translated to English.
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
