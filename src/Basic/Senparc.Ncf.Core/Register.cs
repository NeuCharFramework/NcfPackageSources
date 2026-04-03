using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.AssembleScan;
using Senparc.Ncf.Core.DI;
using System;
using System.Linq;
using System.Reflection;

namespace Senparc.Ncf.Core
{
    /// <summary>
    /// NCF registration program
    /// </summary>
    public static class Register
    {
        /// <summary>
        /// Scan interfaces for auto dependency injection
        /// </summary>
        public static IServiceCollection ScanAssamblesForAutoDI(this IServiceCollection services)
        {
            // Traverse all assemblies for registration
            Action<Assembly> action = assembly =>
            {
                var areaRegisterTypes = assembly.GetTypes() //.GetExportedTypes()
                               .Where(z => !z.IsAbstract && !z.IsInterface && z.GetInterface(nameof(IAutoDI)) != null)
                               .ToArray();

                DILifecycleType dILifecycleType = DILifecycleType.Scoped;

                foreach (var registerType in areaRegisterTypes)
                {
                    try
                    {
                        // Check attribute tags
                        var attrs = System.Attribute.GetCustomAttributes(registerType, false).Where(z => z is AutoDITypeAttribute);
                        if (attrs.Count() > 0)
                        {
                            var attr = attrs.First() as AutoDITypeAttribute;
                            dILifecycleType = attr.DILifecycleType;  // Use specified way
                        }

                        // Set different DI lifecycles for different types
                        switch (dILifecycleType)
                        {
                            case DILifecycleType.Scoped:
                                services.AddScoped(registerType);
                                break;
                            case DILifecycleType.Singleton:
                                services.AddSingleton(registerType);
                                break;
                            case DILifecycleType.Transient:
                                services.AddTransient(registerType);
                                break;
                            default:
                                throw new NotImplementedException($"Unhandled DILifecycleType: {dILifecycleType.ToString()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        SenparcTrace.BaseExceptionLog(ex);
                    }
                }
            };

            AssembleScanHelper.AddAssembleScanItem(action, true);

            return services;
        }


        #region TryRegisterMiniCore
        /// <summary>
        /// Perform auto-registration with minimal process, suitable for unit tests or Code First commands in limited environments. Do not use in production!
        /// <para>If already registered, returns null</para>
        /// </summary>
        public static IRegisterService TryRegisterMiniCore(Action<IServiceCollection> servicesAction = null, Action<IApplicationBuilder> appAction = null)
        {
            //Initialize project
            if (!Senparc.CO2NET.RegisterServices.RegisterServiceExtension.SenparcGlobalServicesRegistered)
            {
                try
                {
                    var repository = LogManager.CreateRepository("NETCoreRepository");//Load Log config file
                    Console.WriteLine("[TryRegisterMiniCore] NETCoreRepository registration completed");
                }
                catch
                {
                    Console.WriteLine("NETCoreRepository already configured, skip duplicate configuration");
                }
                //Allow additional external service registrations
                var services = RegisterServiceCollection();

                Console.WriteLine("[TryRegisterMiniCore] RegisterServiceCollection() 完成");

                servicesAction?.Invoke(services);
                Console.WriteLine("[TryRegisterMiniCore] servicesAction?.Invoke(services) 完成");

                //Allow additional external app configuration
                var serviceProvider = services.BuildServiceProvider();
                Console.WriteLine("[TryRegisterMiniCore] services.BuildServiceProvider() 完成");

                IApplicationBuilder app = new ApplicationBuilder(serviceProvider);
                appAction?.Invoke(app);
                Console.WriteLine("[TryRegisterMiniCore] appAction?.Invoke(app) 完成");


                return RegisterServiceStart();
            }
            return null;
        }

        private static SenparcSetting CreateSenparcSetting()
        {
            return new SenparcSetting() { IsDebug = true };
        }

        /// <summary>
        /// Register IServiceCollection and MemoryCache
        /// </summary>
        private static IServiceCollection RegisterServiceCollection()
        {
            var serviceCollection = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();
            var config = configBuilder.Build();
            serviceCollection.AddSenparcGlobalServices(config);
            serviceCollection.AddMemoryCache();//Use in-memory cache
            return serviceCollection;
        }


        /// <summary>
        /// Register RegisterService.Start()
        /// </summary>
        private static IRegisterService RegisterServiceStart(bool autoScanExtensionCacheStrategies = false)
        {
            Console.WriteLine("[TryRegisterMiniCore] RegisterServiceStart() 开始");

            //Register
            var senparcSetting = CreateSenparcSetting();
            return Senparc.CO2NET.Register.UseSenparcGlobal(senparcSetting, reg => { }, autoScanExtensionCacheStrategies);
        }
        #endregion
    }
}
