using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.AssembleScan;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Database;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Threads;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// Xncf 全局注册类
    /// </summary>
    public static class Register
    {
        /// <summary>
        /// 所有线程的集合
        /// </summary>
        public static ConcurrentDictionary<ThreadInfo, Thread> ThreadCollection { get; set; } = new ConcurrentDictionary<ThreadInfo, Thread>();

        public static FunctionRenderCollection FunctionRenderCollection { get; set; } = new FunctionRenderCollection();

        /// <summary>
        /// 所有自动注册 Xncf 的数据库的 ConfigurationMapping 对象
        /// </summary>
        public static List<object> XncfAutoConfigurationMappingList { get; set; } = new List<object>();
        //public static List<IEntityTypeConfiguration<EntityBase>> XncfAutoConfigurationMappingList = new List<IEntityTypeConfiguration<EntityBase>>();

        /// <summary>
        /// 扫描程序集分类
        /// </summary>
        enum ScanTypeKind
        {
            IXncfRegister,
            //IXncfFunction,
            XncfAutoConfigurationMappingAttribute
        }

        private static void SetLog(StringBuilder sb, string log, bool addTime = true)
        {
            var msg = addTime ? $"[{SystemTime.Now}] {log}" : $"\t{log}";
            sb.AppendLine(msg);
            //Debug.WriteLine(msg);
            //Console.WriteLine(msg);
        }


        ///// <summary>
        ///// 启动 XNCF 模块引擎，包括初始化扫描和注册等过程
        ///// </summary>
        ///// <returns></returns>
        //[Obsolete("请使用 StartNcfEngine()", true)]
        //public static string StartEngine(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        //{
        //    return StartNcfEngine(services, configuration, env, null);
        //}

        /// <summary>
        /// 启动 XNCF 模块引擎，包括初始化扫描和注册等过程
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="env"></param>
        /// <param name="dllFilePatterns">被包含的 dll 的文件名， “.Xncf.”会被必定包含在里面</param>
        /// <returns></returns>
        public static string StartNcfEngine(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env, string[] dllFilePatterns)
        {
            StringBuilder sb = new StringBuilder();
            SetLog(sb, "Start scanning XncfModules");
            var scanTypesCount = 0;
            var hideTypeCount = 0;
            ConcurrentDictionary<Type, ScanTypeKind> types = new ConcurrentDictionary<Type, ScanTypeKind>();

            //所有 XNCF 模块，包括被忽略的。
            //var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();
            //using (cache.BeginCacheLock("Senparc.Ncf.XncfBase.Register", "Scan")) //在注册阶段还未完成缓存配置
            {
                try
                {
                    //遍历所有程序集
                    var assemblies = AssembleScanHelper.GetAssembiles(true, dllFilePatterns: dllFilePatterns);

                    int columnWidth1 = 42;
                    int columnWidth2 = 45;
                    int columnWidth3 = 15;

                    SetLog(sb, " === Multiple databases detected ===");
                    SetLog(sb, $"| {"Register".PadRight(columnWidth1)}| {"Full Name".PadRight(columnWidth2)}| {"Database Type".PadRight(columnWidth3)}", false);
                    SetLog(sb, $"|-{new String('-', columnWidth1)}|-{new String('-', columnWidth2)}|-{new String('-', columnWidth3)}", false);
                    AssembleScanHelper.AddAssembleScanItem(a =>
                    {
                        //Console.WriteLine("FullName:" + a.FullName);
                        if (a.FullName.StartsWith("AutoMapper."))
                        {
                            return;//忽略 AutoMapper
                        }

                        scanTypesCount++;
                        var aTypes = a.GetTypes();

                        //遍历程序集内的所有类型
                        foreach (var t in aTypes)
                        {
                            if (t.IsAbstract)
                            {
                                continue;//忽略抽象类
                            }

                            //Console.WriteLine(t.GetType().Name);
                            //获取 XncfRegister
                            if (t.GetInterfaces().Contains(typeof(IXncfRegister)))
                            {
                                types[t] = ScanTypeKind.IXncfRegister;
                            }
                            //获取 XncfFunction
                            //if (t.GetInterfaces().Contains(typeof(IXncfFunction)))
                            //{
                            //    types[t] = ScanTypeKind.IXncfFunction; /* 暂时不收录处理 */
                            //}
                            //获取 XncfAutoConfigurationMapping
                            if (t.GetCustomAttributes(true).FirstOrDefault(z => z is XncfAutoConfigurationMappingAttribute) != null
                                /*&& t.GetInterfaces().Contains(typeof(IEntityTypeConfiguration<>))*/)
                            {
                                types[t] = ScanTypeKind.XncfAutoConfigurationMappingAttribute;
                            }


                            //获取多数据库配置（XncfDatabaseDbContext 的子类）
                            if (t.IsSubclassOf(typeof(DbContext)) /*t.IsSubclassOf(typeof(XncfDatabaseDbContext))*/ &&
                                t.GetInterface(nameof(ISenparcEntitiesDbContext)) != null &&
                                t.GetCustomAttributes(true).FirstOrDefault(z => z is MultipleMigrationDbContextAttribute) != null)
                            {
                                //获取特性
                                var multiDbContextAttr = t.GetCustomAttributes(true).FirstOrDefault(z => z is MultipleMigrationDbContextAttribute) as MultipleMigrationDbContextAttribute;

                                //添加配置
                                var multipleDatabasePool = MultipleDatabasePool.Instance;
                                var result = multipleDatabasePool.TryAdd(multiDbContextAttr, t, new[] { columnWidth1, columnWidth2, columnWidth3 });
                                SetLog(sb, result, false);
                            }

                            //配置 FunctionRender
                            if (t.IsSubclassOf(typeof(AppServiceBase)))
                            {
                                //遍历其中具体方法
                                var methods = t.GetMethods();
                                var hasFunctionMethod = false;
                                foreach (var method in methods)
                                {
                                    var attr = method.GetCustomAttributes(typeof(FunctionRenderAttribute), true).FirstOrDefault() as FunctionRenderAttribute;
                                    if (attr != null)
                                    {
                                        FunctionRenderCollection.Add(method, attr);
                                        hasFunctionMethod = true;
                                    }
                                }

                                services.AddScoped(t);
                            }

                            //配置 ServiceBase
                            if (
                                !t.IsAbstract && (

                                //由于泛型是在编译时确定的，所以不能直接使用IsSubclassOf方法来判断一个类是否为一个带泛型的类的基类。需要使用GetGenericTypeDefinition方法来获取泛型的基类，然后进行比较。
                                (t.BaseType != null && t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == typeof(ServiceBase<>))
                                || t.IsSubclassOf(typeof(ServiceDataBase))
                                //|| t.IsSubclassOf(typeof(AppServiceBase))
                                //|| t.IsInstanceOfType(typeof(IServiceDataBase))
                                )
                                )
                            {
                                if (t != typeof(ServiceBase<>))
                                {
                                    //Console.WriteLine("------------> Add Scope:" + t.FullName);
                                    services.AddScoped(t);
                                }

                            }
                        }
                    }, true);

                    SetLog(sb, $"{new String('-', columnWidth1 + columnWidth2 + columnWidth3 + 6)}", false);
                    SetLog(sb, "");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"扫描程集异常退出，可能无法获得完整程序集信息：{ex.Message}");
                }

                SetLog(sb, $"Satisfies the ScanTypeKind condition: {types.Count()}");

                //先注册 XncfRegister
                {
                    //筛选
                    var allTypes = types.Where(z => z.Value == ScanTypeKind.IXncfRegister/* && z.Key.GetInterfaces().Contains(typeof(IXncfRegister))*/)
                        .Select(z => z.Key);
                    //按照优先级进行排序（降序，数字越大越在前）
                    var orderedTypes = allTypes.OrderByDescending(z =>
                    {
                        var orderAttribute = z.GetCustomAttributes(true).FirstOrDefault(attr => attr is XncfOrderAttribute) as XncfOrderAttribute;
                        if (orderAttribute != null)
                        {
                            return orderAttribute.Order;
                        }
                        return 0;
                    });


                    foreach (var type in orderedTypes)
                    {
                        SetLog(sb, $"Scan IXncfRegister: {type.FullName}");

                        var register = type.Assembly.CreateInstance(type.FullName) as IXncfRegister;

                        if (!XncfRegisterManager.RegisterList.Contains(register))
                        {
                            if (XncfRegisterManager.RegisterList.Exists(z => z.Uid.Equals(register.Uid, StringComparison.OrdinalIgnoreCase)))
                            {
                                if (DatabaseConfigurationFactory.Instance.Current.MultipleDatabaseType == MultipleDatabaseType.InMemory)
                                {
                                    //单元测试，忽略
                                    Console.WriteLine("MultipleDatabaseType.InMemory 模式，单元测试忽略重复加载");
                                }
                                else
                                {
                                    throw new XncfFunctionException("已经存在相同 Uid 的模块：" + register.Uid);
                                }
                            }

                            if (register.IgnoreInstall)
                            {
                                hideTypeCount++;
                            }
                            XncfRegisterManager.RegisterList.Add(register);//只有允许安装的才进行注册，否则执行完即结束
                            services.AddScoped(type);//DI 中注册
                                                     //foreach (var functionType in register.Functions)
                                                     //{
                                                     //    services.AddScoped(functionType);//DI 中注册
                                                     //}
                        }

                        //初始化数据库
                        if (register is IXncfDatabase)
                        {

                        }

                    }
                }

                //处理 XncfAutoConfigurationMappingAttribute
                {
                    var allTypes = types.Where(z => z.Value == ScanTypeKind.XncfAutoConfigurationMappingAttribute).Select(z => z.Key);
                    foreach (var type in allTypes)
                    {
                        var obj = type.Assembly.CreateInstance(type.FullName);
                        XncfAutoConfigurationMappingList.Add(obj /*as IEntityTypeConfiguration<EntityBase>*/);
                    }
                }
            }

            var scanResult = $"The initialization scan ended, scanning a total of {scanTypesCount} Assemblies.";
            if (hideTypeCount > 0)
            {
                scanResult += $"Among them, {hideTypeCount} assemblies are non-installed assemblies and will not be cached.";
            }
            SetLog(sb, $"{scanResult}");


            //Repository & Service
            services.AddScoped(typeof(Senparc.Ncf.Repository.IRepositoryBase<>), typeof(Senparc.Ncf.Repository.RepositoryBase<>));
            services.AddScoped(typeof(Senparc.Ncf.Repository.RepositoryBase<>));
            services.AddScoped(typeof(Senparc.Ncf.Repository.IClientRepositoryBase<>), typeof(Senparc.Ncf.Repository.ClientRepositoryBase<>));
            services.AddScoped(typeof(Senparc.Ncf.Repository.ClientRepositoryBase<>));
            services.AddScoped(typeof(IServiceBase<>), typeof(ServiceBase<>));
            services.AddScoped(typeof(ServiceBase<>));
            services.AddScoped(typeof(ServiceBase<>));

            //AppService
            services.AddScoped(typeof(AppRequestBase));
            services.AddScoped(typeof(IAppRequest), typeof(AppRequestBase));

            services.AddScoped(typeof(AppResponseBase));
            services.AddScoped(typeof(AppResponseBase<>));
            services.AddScoped(typeof(IAppResponse), typeof(AppResponseBase));
            //services.AddScoped(typeof(IAppResponse<>), typeof(AppResponseBase<>));

            //ConfigurationMapping
            services.AddScoped(typeof(ConfigurationMappingBase<>));
            services.AddScoped(typeof(ConfigurationMappingWithIdBase<,>));


            services.AddScoped(typeof(DbContextOptionsBuilder<>));
            services.AddScoped(typeof(DbContextOptionsBuilder));

            //多租户
            services.AddScoped<RequestTenantInfo>();

            services.ScanAssamblesForAutoDI();
            //已经添加完所有程序集自动扫描的委托，立即执行扫描（必须）
            AssembleScanHelper.RunScan(dllFilePatterns);
            //services.AddSingleton<Core.Cache.RedisProvider.IRedisProvider, Core.Cache.RedisProvider.StackExchangeRedisProvider>();


            #region 支持 AutoMapper

            //Console.WriteLine("----------");
            //Console.WriteLine("XNCF 模块进行 AutoMapper 映射 foreach (var xncfRegister in XncfRegisterManager.RegisterList)");
            //Console.WriteLine("----------");
            //XNCF 模块进行 AutoMapper 映射
            foreach (var xncfRegister in XncfRegisterManager.RegisterList)
            {
                xncfRegister.OnAutoMapMapping(services, configuration);
            }

            //Console.WriteLine("----------");
            //Console.WriteLine("引入当前系统");
            //Console.WriteLine("----------");

            //引入当前系统
            services.AddAutoMapper(z => z.AddProfile<Core.AutoMapper.SystemProfile>());

            //引入所有模块    TODO:XncfModuleProfile 构造函数会在“XNCF 模块进行 AutoMapper 映射”之后就执行，导致 XNCF 模块的 AutoMapper 映射无效
            services.AddAutoMapper(z => z.AddProfile<AutoMapper.XncfModuleProfile>());

            #endregion

            //说明：AutoMapper 需要放到 XNCF 注册之前，因为 XNCF 内可能存在动态生成的程序集引发异常

            //XNCF 模块进行 Service 注册
            foreach (var xncfRegister in XncfRegisterManager.RegisterList)
            {
                try
                {
                    xncfRegister.AddXncfModule(services, configuration, env);
                }
                catch (Exception ex)
                {
                    _ = new NcfExceptionBase($"{xncfRegister.Name} 模块 xncfRegister.AddXncfModule() 出错 ：{ex.Message}", ex);

                }
            }

            //标记所有 AddXncfModule 方法已经执行完成
            Ncf.Core.Config.SiteConfig.NcfCoreState.AllAddXncfModuleApplied = true;
            Ncf.Core.Config.SiteConfig.NcfCoreState.AllDatabaseXncfLoaded = true;

            SetLog(sb, $"Finish services.AddXncfModule(): Total of {scanTypesCount} assemblies were scanned.");

            return sb.ToString();
        }

        /// <summary>
        /// 扫描并安装（自动安装，无需手动）
        /// TODO：放置到 Service 中，为系统模块自动升级
        /// </summary>
        /// <param name="xncfModuleDtos">现有已安装的模块</param>
        /// <param name="serviceProvider">IServiceProvider</param>
        /// <param name="afterInstalledOrUpdated">安装或更新后执行</param>
        /// <param name="justScanThisUid">只扫描并更新特定的Uid</param>
        /// <returns></returns>
        public static async Task<string> ScanAndInstall(IList<CreateOrUpdate_XncfModuleDto> xncfModuleDtos,
            IServiceProvider serviceProvider,
            Func<IXncfRegister, InstallOrUpdate, Task> afterInstalledOrUpdated = null,
            string justScanThisUid = null)
        {
            StringBuilder sb = new StringBuilder();
            SetLog(sb, "开始扫描 XncfModules");

            //先注册
            var updatedCount = 0;
            var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();
            using (await cache.BeginCacheLockAsync("Senparc.Ncf.XncfBase.Register", "Scan").ConfigureAwait(false))
            {
                foreach (var register in XncfRegisterManager.RegisterList)
                {
                    SetLog(sb, $"扫描到 IXncfRegister：{register.GetType().FullName}");
                    if (register.IgnoreInstall)
                    {
                        SetLog(sb, $"当前模块要求忽略安装 uid：[{justScanThisUid}]，此模块跳过");
                        continue;
                    }

                    if (justScanThisUid != null && register.Uid != justScanThisUid)
                    {
                        SetLog(sb, $"由于只要求更新 uid：[{justScanThisUid}]，此模块跳过");
                        continue;
                    }
                    else
                    {
                        SetLog(sb, "符合尝试安装/更新要求，继续执行");
                    }

                    var xncfModuleStoredDto = xncfModuleDtos.FirstOrDefault(z => z.Uid == register.Uid);
                    var xncfModuleAssemblyDto = new UpdateVersion_XncfModuleDto(register.Name, register.Uid, register.MenuName, register.Version, register.Description, register.Icon);

                    //检查更新，并安装到数据库
                    var xncfModuleService = serviceProvider.GetService<XncfModuleService>();
                    var installOrUpdate = await xncfModuleService.CheckAndUpdateVersionAsync(xncfModuleStoredDto, xncfModuleAssemblyDto).ConfigureAwait(false);
                    SetLog(sb, $"是否更新版本：{installOrUpdate?.ToString() ?? "未安装"}");

                    if (installOrUpdate.HasValue)
                    {
                        updatedCount++;

                        //执行安装程序
                        await register.InstallOrUpdateAsync(serviceProvider, installOrUpdate.Value).ConfigureAwait(false);

                        await afterInstalledOrUpdated?.Invoke(register, installOrUpdate.Value);
                    }
                }
            }

            SetLog(sb, $"扫描结束，共新增或更新 {updatedCount} 个程序集");
            return sb.ToString();
        }

        /// <summary>
        /// 通常在 Startup.cs 中的 Configure() 方法中执行
        /// </summary>
        /// <param name="app"></param>
        /// <param name="registerService">CO2NET 注册对象</param>
        /// <param name="senparcCoreSetting">SenparcCoreSetting</param>
        /// <returns></returns>
        public static IApplicationBuilder UseXncfModules<TDatabaseConfiguration>(this IApplicationBuilder app, IRegisterService registerService, SenparcCoreSetting senparcCoreSetting = null, bool autoRunInstall = false)
        where TDatabaseConfiguration : IDatabaseConfiguration, new()
        {
            return UseXncfModules<TDatabaseConfiguration>(app, registerService, senparcCoreSetting, autoRunInstall);
        }


        /// <summary>
        /// 通常在 Startup.cs 中的 Configure() 方法中执行
        /// </summary>
        /// <param name="app"></param>
        /// <param name="registerService">CO2NET 注册对象</param>
        /// <param name="senparcCoreSetting">SenparcCoreSetting</param>
        /// <returns></returns>
        public static IApplicationBuilder UseXncfModules(this IApplicationBuilder app, IRegisterService registerService, SenparcCoreSetting senparcCoreSetting = null, bool autoRunInstall = false)
        {
            if (senparcCoreSetting == null)
            {
                using (var scope = app.ApplicationServices.CreateAsyncScope())
                {
                    senparcCoreSetting = scope.ServiceProvider.GetService<IOptions<SenparcCoreSetting>>()?.Value;
                }
            }

            Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting = senparcCoreSetting;

            foreach (var register in XncfRegisterManager.RegisterList)
            {
                try
                {
                    register.UseXncfModule(app, registerService);

                    //TODO: 后期改为远程（其他模块）查找租户

                    // 是否已经载入过数据库功能的 Database
                    if (!SiteConfig.DatabaseXncfLoaded
                        //SystemCore 需要支持 IXncfDatabase，但并不属于实际运作的数据库模块，因此需要排除
                        && register.Uid != SiteConfig.SYSTEM_XNCF_MODULE_SYSTEM_CORE_UID
                        && register is IXncfDatabase
                        )
                    {
                        SiteConfig.DatabaseXncfLoaded = true;
                    }
                }
                catch
                {
                }

                //执行中间件
                if (register is IXncfMiddleware middlewareRegister)
                {
                    try
                    {
                        middlewareRegister.UseMiddleware(app);
                    }
                    catch
                    {
                    }
                }

                //执行线程
                if (register is IXncfThread threadRegister)
                {
                    try
                    {
                        XncfThreadBuilder xncfThreadBuilder = new XncfThreadBuilder();
                        threadRegister.ThreadConfig(xncfThreadBuilder);
                        xncfThreadBuilder.Build(app, register);
                    }
                    catch (Exception ex)
                    {
                        SenparcTrace.BaseExceptionLog(ex);
                    }

                    //任意一个 ThreadXncf 已经载入
                    Ncf.Core.Config.SiteConfig.NcfCoreState.AnyThreadXncfLoaded = true;
                }
            }

            //所有 ThreadXncf 已经载入
            Ncf.Core.Config.SiteConfig.NcfCoreState.AllThreadXncfLoaded = true;
            //所有 UseXncfModule 已经执行完成
            Ncf.Core.Config.SiteConfig.NcfCoreState.AllUseXncfModuleApplied = true;

            //自动运行安装

            if (autoRunInstall)
            {
                Task.Factory.StartNew(async () =>
                {
                    using (var scope = app.ApplicationServices.CreateScope())
                    {
                        foreach (var register in XncfRegisterManager.RegisterList)
                        {
                            await register.InstallOrUpdateAsync(scope.ServiceProvider, InstallOrUpdate.Install);
                        }
                    }
                }).GetAwaiter().GetResult();
            }

            return app;
        }

        /// <summary>
        /// 所有已经使用的 [AutoConfigurationMapping] 对应的实体类型
        /// </summary>
        public static List<Type> ApplyedAutoConfigurationMappingTypes { get; set; } = new List<Type>();
        private static List<Type> AddedApplyedAutoConfigurationMappingEntityTypes { get; set; } = new List<Type>();
        /// <summary>
        /// 自动添加所有 XNCF 模块中标记了 [XncfAutoConfigurationMapping] 特性的对象
        /// </summary>
        public static void ApplyAllAutoConfigurationMapping(ModelBuilder modelBuilder)
        {
            var entityTypeConfigurationMethod = typeof(ModelBuilder).GetMethods()
                .FirstOrDefault(z => z.Name == "ApplyConfiguration" && z.ContainsGenericParameters && z.GetParameters().SingleOrDefault()?.ParameterType.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));

            //所有模块中数据库实体中自动获取所有的 DbSet 下的实体类型

            //TODO:筛选基础类中的
            var xncfDatabaseDbContextList = MultipleDatabasePool.Instance.Values.SelectMany(z => z.Values);
            //TODO:为了进一步提高效率，可以对同一个 DbContext 的子类去重

            foreach (var xncfDatabaseDbContextType in xncfDatabaseDbContextList)
            {
                var setKeyInfoList = EntitySetKeys.GetEntitySetInfo(xncfDatabaseDbContextType).Values;
                foreach (var setKeyInfo in setKeyInfoList)
                {
                    //数据库实体类型
                    var entityType = setKeyInfo.DbSetType;
                    if (AddedApplyedAutoConfigurationMappingEntityTypes.Contains(entityType))
                    {
                        //Console.WriteLine($"\t [{xncfDatabaseDbContextType.Name}]ApplyAllAutoConfigurationMapping 有重复 setKeyInfo：{entityType.Name}，已跳过");
                        continue;
                    }
                    //Console.WriteLine($"\t [{xncfDatabaseDbContextType.Name}]ApplyAllAutoConfigurationMapping 处理 setKeyInfo：{entityType.Name}");

                    //默认空 ConfigurationMapping 对象的泛型类型
                    var blankEntityTypeConfigurationType = typeof(BlankEntityTypeConfiguration<>).MakeGenericType(entityType);
                    //创建一个新的实例
                    var blankEntityTypeConfiguration = Activator.CreateInstance(blankEntityTypeConfigurationType);
                    //最佳到末尾，这样可以优先执行用户自定义的代码
                    XncfAutoConfigurationMappingList.Add(blankEntityTypeConfiguration);
                    AddedApplyedAutoConfigurationMappingEntityTypes.Add(entityType);
                }
            }

            foreach (var autoConfigurationMapping in XncfAutoConfigurationMappingList)
            {
                if (autoConfigurationMapping == null)
                {
                    continue;
                }

                SenparcTrace.SendCustomLog("监测到 ApplyAllAutoConfigurationMapping 执行", autoConfigurationMapping.GetType().FullName);

                //获取配置实体类型，如：DbConfig_WeixinUserConfigurationMapping
                Type mappintConfigType = autoConfigurationMapping.GetType();
                //获取 IEntityTypeConfiguration<Entity> 接口
                var interfaceType = mappintConfigType.GetInterfaces().FirstOrDefault(z => z.Name.StartsWith("IEntityTypeConfiguration"));
                if (interfaceType == null)
                {
                    Console.WriteLine("interfaceType 为 null（不为 IEntityTypeConfiguration）");
                    continue;
                }
                //实体类型，如：DbConfig
                Type entityType = interfaceType.GenericTypeArguments[0];

                //PS：此处不能过滤，否则可能导致如 SystemServiceEntities_SqlServer / SystemServiceEntities_Mysql 中只有先注册的对象才成功，后面的被忽略
                //if (ApplyedAutoConfigurationMappingTypes.Contains(entityType))
                //{
                //    //如果已经添加过则跳过。作此判断因为：原始的 XncfAutoConfigurationMappingList 数据可能和上一步自动添加 DataSet 中的对象有重复
                //    //continue;
                //}

                entityTypeConfigurationMethod.MakeGenericMethod(entityType)
                                .Invoke(modelBuilder, new object[1]
                                {
                                    autoConfigurationMapping
                                });

                ApplyedAutoConfigurationMappingTypes.Add(entityType);

                //entityTypeConfigurationMethod.Invoke(modelBuilder, new[] { autoConfigurationMapping });
            }

            //TODO：添加 IQueryTypeConfiguration<>


        }
    }
}
