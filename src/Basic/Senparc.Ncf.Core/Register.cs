using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.AssembleScan;
using Senparc.Ncf.Core.DI;
using System;
using System.Linq;

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
            AssembleScanHelper.AddAssembleScanItem(assembly =>
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
            }, false);

            return services;
        }



        #region TryRegisterMiniCore
        /// <summary>
        /// 以最小化的过程进行自动注册，适用于缺少环境的单元测试、Code First 命令等。请勿在生产环境中使用此命令！
        /// <para>如果已经注册过，则返回 null</para>
        /// </summary>
        public static IRegisterService TryRegisterMiniCore()
        {
            //初始化项目
            if (!Senparc.CO2NET.RegisterServices.RegisterServiceExtension.SenparcGlobalServicesRegistered)
            {
                try
                {
                    var repository = LogManager.CreateRepository("NETCoreRepository");//读取Log配置文件
                }
                catch
                {
                    Console.WriteLine("NETCoreRepository 已进行过配置，无需重复配置");
                }
                RegisterServiceCollection();
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
        private static void RegisterServiceCollection()
        {
            var serviceCollection = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();
            var config = configBuilder.Build();
            serviceCollection.AddSenparcGlobalServices(config);
            serviceCollection.AddMemoryCache();//使用内存缓存
        }


        /// <summary>
        /// 注册 RegisterService.Start()
        /// </summary>
        private static IRegisterService RegisterServiceStart(bool autoScanExtensionCacheStrategies = false)
        {
            //注册
            var senparcSetting = CreateSenparcSetting();
            return Senparc.CO2NET.Register.UseSenparcGlobal(senparcSetting, reg => { });
        }
        #endregion
    }
}
