/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

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
    /// Provides minimal dependency-injection and Senparc global-service registration helpers.
    /// </summary>
    public static class Register
    {
        /// <summary>
        /// Scans loaded assemblies for <see cref="IAutoDI"/> implementations and registers
        /// them using their declared lifecycle.
        /// </summary>
        /// <param name="services">The service collection receiving discovered registrations.</param>
        /// <returns>The same service collection.</returns>
        public static IServiceCollection ScanAssamblesForAutoDI(this IServiceCollection services)
        {
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
                        var attrs = System.Attribute.GetCustomAttributes(registerType, false).Where(z => z is AutoDITypeAttribute);
                        if (attrs.Count() > 0)
                        {
                            var attr = attrs.First() as AutoDITypeAttribute;
                            dILifecycleType = attr.DILifecycleType;
                        }

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
                                throw new NotImplementedException($"未处理此 DILifecycleType 类型：{dILifecycleType.ToString()}");
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
        /// Registers a lightweight Senparc service provider and optionally applies caller
        /// service/application configuration actions.
        /// </summary>
        /// <param name="servicesAction">Optional service-registration callback.</param>
        /// <param name="appAction">Optional application-builder callback.</param>
        /// <returns>The started registration service, or <see langword="null"/> when global services were already registered.</returns>
        public static IRegisterService TryRegisterMiniCore(Action<IServiceCollection> servicesAction = null, Action<IApplicationBuilder> appAction = null)
        {
            if (!Senparc.CO2NET.RegisterServices.RegisterServiceExtension.SenparcGlobalServicesRegistered)
            {
                try
                {
                    var repository = LogManager.CreateRepository("NETCoreRepository");//readLogconfigurationfile
                    Console.WriteLine("[TryRegisterMiniCore] NETCoreRepository 注册完成");
                }
                catch
                {
                    Console.WriteLine("NETCoreRepository 已进行过配置，无需重复配置");
                }
                var services = RegisterServiceCollection();

                Console.WriteLine("[TryRegisterMiniCore] RegisterServiceCollection() 完成");

                servicesAction?.Invoke(services);
                Console.WriteLine("[TryRegisterMiniCore] servicesAction?.Invoke(services) 完成");

                var serviceProvider = services.BuildServiceProvider();
                Console.WriteLine("[TryRegisterMiniCore] services.BuildServiceProvider() 完成");

                IApplicationBuilder app = new ApplicationBuilder(serviceProvider);
                appAction?.Invoke(app);
                Console.WriteLine("[TryRegisterMiniCore] appAction?.Invoke(app) 完成");


                return RegisterServiceStart();
            }
            return null;
        }

        /// <summary>
        /// Creates the default Senparc setting used by the minimal registration path.
        /// </summary>
        /// <returns>A debug-enabled default setting.</returns>
        private static SenparcSetting CreateSenparcSetting()
        {
            return new SenparcSetting() { IsDebug = true };
        }

        /// <summary>Creates a minimal service collection with global services and memory caching.</summary>
        private static IServiceCollection RegisterServiceCollection()
        {
            var serviceCollection = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();
            var config = configBuilder.Build();
            serviceCollection.AddSenparcGlobalServices(config);
            serviceCollection.AddMemoryCache();
            return serviceCollection;
        }


        /// <summary>
        /// Starts Senparc global services using the minimal registration setting.
        /// </summary>
        /// <param name="autoScanExtensionCacheStrategies">
        /// Whether cache-strategy implementations should be discovered automatically.
        /// </param>
        /// <returns>The started global registration service.</returns>
        private static IRegisterService RegisterServiceStart(bool autoScanExtensionCacheStrategies = false)
        {
            Console.WriteLine("[TryRegisterMiniCore] RegisterServiceStart() 开始");

            var senparcSetting = CreateSenparcSetting();
            return Senparc.CO2NET.Register.UseSenparcGlobal(senparcSetting, reg => { }, autoScanExtensionCacheStrategies);
        }
        #endregion
    }
}
