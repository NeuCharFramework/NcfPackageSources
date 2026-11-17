/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: Register.cs

    File description: Register related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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
    ///Legacy comment translated to English.
    /// </summary>
    public static class Register
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static IServiceCollection ScanAssamblesForAutoDI(this IServiceCollection services)
        {
            //Legacy comment translated to English.
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
                        //Legacy comment translated to English.
                        var attrs = System.Attribute.GetCustomAttributes(registerType, false).Where(z => z is AutoDITypeAttribute);
                        if (attrs.Count() > 0)
                        {
                            var attr = attrs.First() as AutoDITypeAttribute;
                            dILifecycleType = attr.DILifecycleType;//Legacy comment translated to English.
                        }

                        //Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public static IRegisterService TryRegisterMiniCore(Action<IServiceCollection> servicesAction = null, Action<IApplicationBuilder> appAction = null)
        {
            //Legacy comment translated to English.
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
                //Legacy comment translated to English.
                var services = RegisterServiceCollection();

                Console.WriteLine("[TryRegisterMiniCore] RegisterServiceCollection() 完成");

                servicesAction?.Invoke(services);
                Console.WriteLine("[TryRegisterMiniCore] servicesAction?.Invoke(services) 完成");

                //Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        private static IServiceCollection RegisterServiceCollection()
        {
            var serviceCollection = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();
            var config = configBuilder.Build();
            serviceCollection.AddSenparcGlobalServices(config);
            serviceCollection.AddMemoryCache();//Legacy comment translated to English.
            return serviceCollection;
        }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private static IRegisterService RegisterServiceStart(bool autoScanExtensionCacheStrategies = false)
        {
            Console.WriteLine("[TryRegisterMiniCore] RegisterServiceStart() 开始");

            //Legacy comment translated to English.
            var senparcSetting = CreateSenparcSetting();
            return Senparc.CO2NET.Register.UseSenparcGlobal(senparcSetting, reg => { }, autoScanExtensionCacheStrategies);
        }
        #endregion
    }
}
