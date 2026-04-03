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
        /// Register Senparc EventBus and all event handlers
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configureOptions">Delegate used to configure EventBus options (optional)</param>
        /// <param name="assembliesToScan">Assemblies to scan for event handlers</param>
        public static IServiceCollection AddSenparcEventBus(
            this IServiceCollection services, 
            Action<EventBusOptions> configureOptions = null,
            params Assembly[] assembliesToScan)
        {
            // 1. Register EventBus options
            var options = new EventBusOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(options);

            // 2. Register singleton EventBus (for publishers) using factory method to support ILogger injection
            services.AddSingleton<InMemoryEventBus>(sp =>
            {
                var logger = sp.GetService<ILogger<InMemoryEventBus>>();
                return new InMemoryEventBus(logger);
            });
            services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<InMemoryEventBus>());

            // 3. Register background hosted service (for consumers)
            services.AddHostedService<EventBusHostedService>();

            // 4. Scan and register handlers
            if (assembliesToScan != null && assembliesToScan.Length > 0)
            {
                foreach (var assembly in assembliesToScan)
                {
                    // Find concrete classes implementing IIntegrationEventHandler<T>
                    var handlerTypes = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract &&
                               t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)));

                    foreach (var type in handlerTypes)
                    {
                        var interfaces = type.GetInterfaces()
                            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>));

                        foreach (var i in interfaces)
                        {
                            // Register as Scoped
                            services.AddScoped(i, type);
                        }
                    }
                }
            }

            return services;
        }
    }
}