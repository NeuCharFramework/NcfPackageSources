using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
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
    /// NCF 的注册程序
    /// </summary>
    public static class Register
    {
        /// <summary>
        /// 扫描自动依赖注入的接口
        /// </summary>
        public static IServiceCollection ScanAssamblesForAutoDI(this IServiceCollection services)
        {
            //遍历所有程序集进行注册
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
                        //判断特性标签
                        var attrs = System.Attribute.GetCustomAttributes(registerType, false).Where(z => z is AutoDITypeAttribute);
                        if (attrs.Count() > 0)
                        {
                            var attr = attrs.First() as AutoDITypeAttribute;
                            dILifecycleType = attr.DILifecycleType;//使用指定的方式
                        }

                        //针对不同的类型进行不同生命周期的 DI 设置
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
        /// 以最小化的过程进行自动注册，适用于缺少环境的单元测试、Code First 命令等。请勿在生产环境中使用此命令！
        /// <para>如果已经注册过，则返回 null</para>
        /// </summary>
        public static IRegisterService TryRegisterMiniCore(Action<IServiceCollection> servicesAction = null, Action<IApplicationBuilder> appAction = null)
        {
            //初始化项目
            if (!Senparc.CO2NET.RegisterServices.RegisterServiceExtension.SenparcGlobalServicesRegistered)
            {
                try
                {
                    var repository = LogManager.CreateRepository("NETCoreRepository");//读取Log配置文件
                    Console.WriteLine("[TryRegisterMiniCore] NETCoreRepository 注册完成");
                }
                catch
                {
                    Console.WriteLine("NETCoreRepository 已进行过配置，无需重复配置");
                }
                //允许从外部添加对 services 的注册操作
                var services = RegisterServiceCollection();

                Console.WriteLine("[TryRegisterMiniCore] RegisterServiceCollection() 完成");

                servicesAction?.Invoke(services);
                Console.WriteLine("[TryRegisterMiniCore] servicesAction?.Invoke(services) 完成");

                //允许从外部添加对 app 的注册操作
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
        /// 注册 IServiceCollection 和 MemoryCache
        /// </summary>
        private static IServiceCollection RegisterServiceCollection()
        {
            var serviceCollection = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();
            var config = configBuilder.Build();
            serviceCollection.AddSenparcGlobalServices(config);
            serviceCollection.AddMemoryCache();//使用内存缓存
            return serviceCollection;
        }


        /// <summary>
        /// 注册 RegisterService.Start()
        /// </summary>
        private static IRegisterService RegisterServiceStart(bool autoScanExtensionCacheStrategies = false)
        {
            Console.WriteLine("[TryRegisterMiniCore] RegisterServiceStart() 开始");

            //注册
            var senparcSetting = CreateSenparcSetting();
            return Senparc.CO2NET.Register.UseSenparcGlobal(senparcSetting, reg => { }, autoScanExtensionCacheStrategies);
        }
        #endregion
    }
}
