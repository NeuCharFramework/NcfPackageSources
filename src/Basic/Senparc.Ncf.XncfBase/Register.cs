/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.24.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.25.1-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.26.0 Updated module behavior and related support.

    Modified by: Senparc - 20261002
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.28.1 add bulk file upload functionality to Sandbox Lab

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.29.1.2 Extend shared chat-tool contracts for admin task initiation.

----------------------------------------------------------------*/

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
using Senparc.Ncf.XncfBase.Functions.Files;
using Senparc.Ncf.XncfBase.Threads;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// Provides global XNCF registration state, function metadata, thread startup,
    /// and automatic Entity Framework configuration mapping services.
    /// </summary>
    public static class Register
    {
        /// <summary>
        /// Gets or sets the background threads registered by XNCF modules.
        /// </summary>
        public static ConcurrentDictionary<ThreadInfo, Thread> ThreadCollection { get; set; } = new ConcurrentDictionary<ThreadInfo, Thread>();

        /// <summary>
        /// Gets or sets the FunctionRender metadata registry discovered during assembly scanning.
        /// </summary>
        public static FunctionRenderCollection FunctionRenderCollection { get; set; } = new FunctionRenderCollection();

        /// <summary>
        /// Gets or sets the entity-configuration objects discovered from XNCF mappings.
        /// </summary>
        public static List<object> XncfAutoConfigurationMappingList { get; set; } = new List<object>();
        //public static List<IEntityTypeConfiguration<EntityBase>> XncfAutoConfigurationMappingList = new List<IEntityTypeConfiguration<EntityBase>>();

        /// <summary>
        /// Identifies the kinds of types discovered during XNCF assembly scanning.
        /// </summary>
        enum ScanTypeKind
        {
            IXncfRegister,
            //IXncfFunction,
            XncfAutoConfigurationMappingAttribute
        }

        /// <summary>
        /// Appends a timestamped or indented diagnostic line to a registration log.
        /// </summary>
        /// <param name="sb">The log builder receiving the line.</param>
        /// <param name="log">The message text.</param>
        /// <param name="addTime">Whether to prefix the message with the current system time.</param>
        private static void SetLog(StringBuilder sb, string log, bool addTime = true)
        {
            var msg = addTime ? $"[{SystemTime.Now}] {log}" : $"\t{log}";
            sb.AppendLine(msg);
            //Debug.WriteLine(msg);
            //Console.WriteLine(msg);
        }

        /// <summary>
        /// Resolves the current database provider from the active configuration,
        /// falling back to SiteConfig and raw configuration values when necessary.
        /// </summary>
        /// <param name="configuration">The application configuration fallback source.</param>
        /// <returns>The resolved database provider, or <see langword="null"/> when unavailable.</returns>
        private static MultipleDatabaseType? ResolveCurrentDatabaseTypeForScanLog(IConfiguration configuration)
        {
            try
            {
                return DatabaseConfigurationFactory.Instance.Current.MultipleDatabaseType;
            }
            catch
            {
            }

            var fromSiteConfig = SiteConfig.SenparcCoreSetting?.DatabaseType;
            if (fromSiteConfig.HasValue)
            {
                return fromSiteConfig;
            }

            var dbTypeText = configuration?["SenparcCoreSetting:DatabaseType"];
            if (!string.IsNullOrWhiteSpace(dbTypeText) &&
                Enum.TryParse<MultipleDatabaseType>(dbTypeText.Trim(), ignoreCase: true, out var parsed))
            {
                return parsed;
            }

            return null;
        }


        // The former StartEngine overload was superseded by StartNcfEngine and
        // is retained only in source history for compatibility reference.
        //public static string StartEngine(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        //{
        //    return StartNcfEngine(services, configuration, env, null);
        //}

        /// <summary>
        /// Scans assemblies for XNCF registrations, configures discovered services,
        /// and prepares the application for module startup.
        /// </summary>
        /// <param name="services">The application service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="env">The hosting environment.</param>
        /// <param name="dllFilePatterns">Optional assembly-name filters used during scanning.</param>
        /// <returns>A diagnostic startup log.</returns>
        public static string StartNcfEngine(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env, string[] dllFilePatterns)
        {
            StringBuilder sb = new StringBuilder();
            SetLog(sb, "Start scanning XncfModules");

            Senparc.Ncf.Core.VersionManager.ShowSuccessTip($"\t启动前自检开始 {SystemTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}", null, false);

            var scanTypesCount = 0;
            var hideTypeCount = 0;
            ConcurrentDictionary<Type, ScanTypeKind> types = new ConcurrentDictionary<Type, ScanTypeKind>();

            //Legacy comment translated to English.
            //var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();
            //Legacy comment translated to English.
            {
                try
                {
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    var assemblies = AssembleScanHelper.GetAssembiles(true, dllFilePatterns: dllFilePatterns);
                    _ = assemblies.Count; //Legacy comment translated to English.

                    AssembleScanHelper.AddAssembleScanItem(a =>
                    {
                        //Console.WriteLine("FullName:" + a.FullName);
                        if (a.FullName.StartsWith("AutoMapper."))
                        {
                            return;//Legacy comment translated to English.
                        }

                        scanTypesCount++;
                        var aTypes = a.GetTypes();

                        List<Exception> exceptions = new List<Exception>();

                        //Legacy comment translated to English.
                        foreach (var t in aTypes)
                        {
                            try
                            {
                                if (t.IsAbstract)
                                {
                                    continue;//Legacy comment translated to English.
                                }

                                //Console.WriteLine(t.GetType().Name);
                                //get XncfRegister
                                if (t.GetInterfaces().Contains(typeof(IXncfRegister)))
                                {
                                    types[t] = ScanTypeKind.IXncfRegister;
                                }
                                //get XncfFunction
                                //if (t.GetInterfaces().Contains(typeof(IXncfFunction)))
                                //{
                                //Legacy comment translated to English.
                                //}
                                //get XncfAutoConfigurationMapping
                                if (t.GetCustomAttributes(true).FirstOrDefault(z => z is XncfAutoConfigurationMappingAttribute) != null
                                    /*&& t.GetInterfaces().Contains(typeof(IEntityTypeConfiguration<>))*/)
                                {
                                    types[t] = ScanTypeKind.XncfAutoConfigurationMappingAttribute;
                                }


                                //Legacy comment translated to English.
                                if (t.IsSubclassOf(typeof(DbContext)) /*t.IsSubclassOf(typeof(XncfDatabaseDbContext))*/ &&
                                    t.GetInterface(nameof(ISenparcEntitiesDbContext)) != null &&
                                    t.GetCustomAttributes(true).FirstOrDefault(z => z is MultipleMigrationDbContextAttribute) != null)
                                {
                                    //Legacy comment translated to English.
                                    var multiDbContextAttr = t.GetCustomAttributes(true).FirstOrDefault(z => z is MultipleMigrationDbContextAttribute) as MultipleMigrationDbContextAttribute;

                                    //Legacy comment translated to English.
                                    MultipleDatabasePool.Instance.TryAdd(multiDbContextAttr, t);
                                }

                                //configuration FunctionRender
                                if (t.IsSubclassOf(typeof(AppServiceBase)))
                                {
                                    //Legacy comment translated to English.
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

                                //configuration ServiceBase
                                if (
                                    !t.IsAbstract && (

                                    //Legacy comment translated to English.
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
                            catch (Exception ex)
                            {
                                exceptions.Add(ex);
                                SenparcTrace.SendCustomLog("扫描程序集发生错误", $"Type:{t.FullName}");
                            }
                        }

                        if (exceptions.Count > 0)
                        {
                            var errMsg = $"程序集 [{a.FullName}] 共检测出 {exceptions.Count} 个异常，如果非核心程序集可忽略";
                            Console.WriteLine(errMsg);
                            SenparcTrace.SendCustomLog("程序集扫描异常记录", errMsg);
                        }

                    }, true);

                    //Legacy comment translated to English.
                    var currentDatabaseType = ResolveCurrentDatabaseTypeForScanLog(configuration);
                    var matrixLog = MultipleDatabasePool.Instance.BuildSupportMatrixLog(currentDatabaseType);
                    foreach (var line in matrixLog.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                    {
                        SetLog(sb, line, addTime: false);
                    }
                    SetLog(sb, "");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"扫描程集异常退出，可能无法获得完整程序集信息：{ex.Message}");
                }

                SetLog(sb, $"Satisfies the ScanTypeKind condition: {types.Count()}");

                //Legacy comment translated to English.
                {
                    //Legacy comment translated to English.
                    var allTypes = types.Where(z => z.Value == ScanTypeKind.IXncfRegister/* && z.Key.GetInterfaces().Contains(typeof(IXncfRegister))*/)
                        .Select(z => z.Key);
                    //Legacy comment translated to English.
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
                                MultipleDatabaseType? multipleDatabaseType = null;
                                try
                                {
                                    multipleDatabaseType = DatabaseConfigurationFactory.Instance.Current.MultipleDatabaseType;
                                }
                                catch (Exception ex)
                                {
                                    SenparcTrace.BaseExceptionLog(ex);
                                }

                                if (multipleDatabaseType == null)
                                {
                                    Console.WriteLine($"MultipleDatabaseType 为 null（轮询${type.FullName}）");
                                }
                                else if (multipleDatabaseType == MultipleDatabaseType.InMemory)
                                {
                                    //Legacy comment translated to English.
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
                            XncfRegisterManager.RegisterList.Add(register);//Legacy comment translated to English.
                            services.AddScoped(type);//Legacy comment translated to English.
                                                     //foreach (var functionType in register.Functions)
                                                     //{
                                                     //Legacy comment translated to English.
                                                     //}
                        }

                        //initialize the database
                        if (register is IXncfDatabase)
                        {

                        }

                    }
                }

                //handling XncfAutoConfigurationMappingAttribute
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
            services.AddOptions<FunctionFileStoreOptions>();
            if (!services.Any(descriptor => descriptor.ServiceType == typeof(IFunctionFileStore)))
            {
                services.AddScoped<IFunctionFileStore, LocalFunctionFileStore>();
            }

            services.AddScoped(typeof(AppResponseBase));
            services.AddScoped(typeof(AppResponseBase<>));
            services.AddScoped(typeof(IAppResponse), typeof(AppResponseBase));
            //services.AddScoped(typeof(IAppResponse<>), typeof(AppResponseBase<>));

            //ConfigurationMapping
            services.AddScoped(typeof(ConfigurationMappingBase<>));
            services.AddScoped(typeof(ConfigurationMappingWithIdBase<,>));


            services.AddScoped(typeof(DbContextOptionsBuilder<>));
            services.AddScoped(typeof(DbContextOptionsBuilder));

            //multi-tenancy
            services.AddScoped<RequestTenantInfo>();//Legacy comment translated to English.
            services.AddSingleton<IBackgroundTenantScopeFactory, BackgroundTenantScopeFactory>();

            //Legacy comment translated to English.
            services.AddScoped<SysButtonService>();
            services.AddScoped<SysMenuService>();
            services.AddScoped<SysRoleAdminUserInfoService>();
            services.AddScoped<SysRolePermissionService>();
            services.AddScoped<SysRoleService>();

            services.ScanAssamblesForAutoDI();
            //Legacy comment translated to English.
            AssembleScanHelper.RunScan(dllFilePatterns);
            //services.AddSingleton<Core.Cache.RedisProvider.IRedisProvider, Core.Cache.RedisProvider.StackExchangeRedisProvider>();


            #region Legacy section

            //Console.WriteLine("----------");
            //Legacy comment translated to English.
            //Console.WriteLine("----------");
            //Legacy comment translated to English.
            foreach (var xncfRegister in XncfRegisterManager.RegisterList)
            {
                xncfRegister.OnAutoMapMapping(services, configuration);
            }

            //Console.WriteLine("----------");
            //Legacy comment translated to English.
            //Console.WriteLine("----------");

            //Legacy comment translated to English.
            services.AddAutoMapper(z => z.AddProfile<Core.AutoMapper.SystemProfile>());

            //Legacy comment translated to English.
            services.AddAutoMapper(z => z.AddProfile<AutoMapper.XncfModuleProfile>());

            #endregion

            //Legacy comment translated to English.

            //Legacy comment translated to English.
            foreach (var xncfRegister in XncfRegisterManager.RegisterList)
            {
                try
                {
                    xncfRegister.AddXncfModule(services, configuration, env);

                    //MCP
                    if (xncfRegister.EnableMcpServer)
                    {
                        xncfRegister.AddMcpServer(services, xncfRegister);
                    }
                }
                catch (Exception ex)
                {
                    _ = new NcfExceptionBase($"{xncfRegister.Name} 模块 xncfRegister.AddXncfModule() 出错 ：{ex.Message}", ex);

                }
            }

            //Legacy comment translated to English.
            Ncf.Core.Config.SiteConfig.NcfCoreState.AllAddXncfModuleApplied = true;
            Ncf.Core.Config.SiteConfig.NcfCoreState.AllDatabaseXncfLoaded = true;

            SetLog(sb, $"Finish services.AddXncfModule(): Total of {scanTypesCount} assemblies were scanned.");

            return sb.ToString();
        }

        /// <summary>
        /// Scans registered modules, compares their persisted versions with the current
        /// registrations, and executes installation or upgrade callbacks for changes.
        /// </summary>
        /// <param name="xncfModuleDtos">The persisted module records used for version comparison.</param>
        /// <param name="serviceProvider">The application service provider used by module callbacks.</param>
        /// <param name="afterInstalledOrUpdated">An optional callback invoked after each changed module.</param>
        /// <param name="justScanThisUid">An optional UID restricting the scan to one module.</param>
        /// <returns>A diagnostic log describing the scan and lifecycle decisions.</returns>
        public static async Task<string> ScanAndInstall(IList<CreateOrUpdate_XncfModuleDto> xncfModuleDtos,
            IServiceProvider serviceProvider,
            Func<IXncfRegister, InstallOrUpdate, Task> afterInstalledOrUpdated = null,
            string justScanThisUid = null)
        {
            StringBuilder sb = new StringBuilder();
            SetLog(sb, "开始扫描 XncfModules");

            //Legacy comment translated to English.
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

                    //Legacy comment translated to English.
                    var xncfModuleService = serviceProvider.GetService<XncfModuleService>();
                    var installOrUpdate = await xncfModuleService.CheckAndUpdateVersionAsync(xncfModuleStoredDto, xncfModuleAssemblyDto).ConfigureAwait(false);
                    SetLog(sb, $"是否更新版本：{installOrUpdate?.ToString() ?? "未安装"}");

                    if (installOrUpdate.HasValue)
                    {
                        updatedCount++;

                        //Legacy comment translated to English.
                        await register.InstallOrUpdateAsync(serviceProvider, installOrUpdate.Value).ConfigureAwait(false);

                        await afterInstalledOrUpdated?.Invoke(register, installOrUpdate.Value);
                    }
                }
            }

            SetLog(sb, $"扫描结束，共新增或更新 {updatedCount} 个程序集");
            return sb.ToString();
        }

        // The generic UseXncfModules overload was superseded by the provider-neutral
        // overload above and is retained only as historical reference.
        //public static IApplicationBuilder UseXncfModules<TDatabaseConfiguration>(this IApplicationBuilder app, IRegisterService registerService, SenparcCoreSetting senparcCoreSetting = null, bool autoRunInstall = false)
        //where TDatabaseConfiguration : IDatabaseConfiguration, new()
        //{
        //    return UseXncfModules<TDatabaseConfiguration>(app, registerService, senparcCoreSetting, autoRunInstall);
        //}


        /// <summary>
        /// Configures the XNCF module middleware, optionally runs installation checks,
        /// and optionally starts registered background threads.
        /// </summary>
        /// <param name="app">The ASP.NET Core application builder.</param>
        /// <param name="registerService">The shared NCF registration service.</param>
        /// <param name="senparcCoreSetting">Optional core settings; loaded from DI when omitted.</param>
        /// <param name="autoRunInstall">Whether module installation/update checks should run immediately.</param>
        /// <param name="startBackgroundThreads">Whether registered module threads should start.</param>
        /// <returns>The same application builder after module initialization.</returns>
        public static IApplicationBuilder UseXncfModules(
            this IApplicationBuilder app,
            IRegisterService registerService,
            SenparcCoreSetting senparcCoreSetting = null,
            bool autoRunInstall = false,
            bool startBackgroundThreads = true)
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

                    //Legacy comment translated to English.

                    //Legacy comment translated to English.
                    if (!SiteConfig.DatabaseXncfLoaded
                        //Legacy comment translated to English.
                        && register.Uid != SiteConfig.SYSTEM_XNCF_MODULE_SYSTEM_CORE_UID
                        && register is IXncfDatabase
                        )
                    {
                        SiteConfig.DatabaseXncfLoaded = true;
                    }
                }
                catch (Exception ex)
                {
                    SenparcTrace.SendCustomLog("从 XncfRegisterManager.RegisterList 执行 register.UseXncfModule() 方法发生异常",
                        $@"模块：{register.Name}
异常信息：{ex.Message}
详情：{ex.ToString()}");
                }

                //Legacy comment translated to English.
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

                //Legacy comment translated to English.
                if (startBackgroundThreads)
                {
                    StartXncfThread(app, register);
                }


                //Legacy comment translated to English.
                if (register.EnableMcpServer)
                {
                    register.UseMcpServer(app, registerService);
                }
            }

            //Legacy comment translated to English.
            Ncf.Core.Config.SiteConfig.NcfCoreState.AllThreadXncfLoaded = startBackgroundThreads;
            //all UseXncfModule alreadyexecutecomplete
            Ncf.Core.Config.SiteConfig.NcfCoreState.AllUseXncfModuleApplied = true;

            //Legacy comment translated to English.

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

        private static readonly object XncfThreadStartLock = new object();

        /// <summary>
        /// Starts registered XNCF background threads once for the application.
        /// Repeated calls return without starting duplicate threads.
        /// </summary>
        public static IApplicationBuilder StartXncfThreads(this IApplicationBuilder app)
        {
            lock (XncfThreadStartLock)
            {
                if (Ncf.Core.Config.SiteConfig.NcfCoreState.AllThreadXncfLoaded)
                {
                    return app;
                }

                foreach (var register in XncfRegisterManager.RegisterList)
                {
                    StartXncfThread(app, register);
                }

                Ncf.Core.Config.SiteConfig.NcfCoreState.AllThreadXncfLoaded = true;
            }

            return app;
        }

        /// <summary>
        /// Configures and starts one module's background thread collection.
        /// Thread-configuration failures are logged and isolated from other modules.
        /// </summary>
        /// <param name="app">The application builder used by the thread runtime.</param>
        /// <param name="register">The module registration to inspect.</param>
        private static void StartXncfThread(IApplicationBuilder app, IXncfRegister register)
        {
            if (register is not IXncfThread threadRegister)
            {
                return;
            }

            try
            {
                var xncfThreadBuilder = new XncfThreadBuilder();
                threadRegister.ThreadConfig(xncfThreadBuilder);
                xncfThreadBuilder.Build(app, register);
                Ncf.Core.Config.SiteConfig.NcfCoreState.AnyThreadXncfLoaded = true;
            }
            catch (Exception ex)
            {
                SenparcTrace.BaseExceptionLog(ex);
            }
        }

        /// <summary>
        /// Stores the entity types whose automatic configuration mappings have been applied.
        /// </summary>
        public static List<Type> ApplyedAutoConfigurationMappingTypes { get; set; } = new List<Type>();
        private static List<Type> AddedApplyedAutoConfigurationMappingEntityTypes { get; set; } = new List<Type>();
        /// <summary>
        /// Discovers database entity sets across registered providers and adds blank
        /// configuration mappings once for each entity type.
        /// </summary>
        /// <param name="modelBuilder">The model builder receiving discovered mappings.</param>
        public static void ApplyAllAutoConfigurationMapping(ModelBuilder modelBuilder)
        {
            var entityTypeConfigurationMethod = typeof(ModelBuilder).GetMethods()
                .FirstOrDefault(z => z.Name == "ApplyConfiguration" && z.ContainsGenericParameters && z.GetParameters().SingleOrDefault()?.ParameterType.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));

            //Legacy comment translated to English.

            //Legacy comment translated to English.
            var xncfDatabaseDbContextList = MultipleDatabasePool.Instance.Values.SelectMany(z => z.Values);
            //Legacy comment translated to English.

            foreach (var xncfDatabaseDbContextType in xncfDatabaseDbContextList)
            {
                var setKeyInfoList = EntitySetKeys.GetEntitySetInfo(xncfDatabaseDbContextType).Values;
                foreach (var setKeyInfo in setKeyInfoList)
                {
                    //Legacy comment translated to English.
                    var entityType = setKeyInfo.DbSetType;
                    if (AddedApplyedAutoConfigurationMappingEntityTypes.Contains(entityType))
                    {
                        //Legacy comment translated to English.
                        continue;
                    }
                    //Console.WriteLine($"\t [{xncfDatabaseDbContextType.Name}]ApplyAllAutoConfigurationMapping handling setKeyInfo：{entityType.Name}");

                    //Legacy comment translated to English.
                    var blankEntityTypeConfigurationType = typeof(BlankEntityTypeConfiguration<>).MakeGenericType(entityType);
                    //Legacy comment translated to English.
                    var blankEntityTypeConfiguration = Activator.CreateInstance(blankEntityTypeConfigurationType);
                    //Legacy comment translated to English.
                    XncfAutoConfigurationMappingList.Add(blankEntityTypeConfiguration);
                    AddedApplyedAutoConfigurationMappingEntityTypes.Add(entityType);
                }
            }

            var logs = new StringBuilder();

            try
            {

                foreach (var autoConfigurationMapping in XncfAutoConfigurationMappingList)
                {
                    if (autoConfigurationMapping == null)
                    {
                        continue;
                    }

                    logs.AppendLine(autoConfigurationMapping.GetType().FullName);

                    //Legacy comment translated to English.
                    Type mappintConfigType = autoConfigurationMapping.GetType();
                    //get IEntityTypeConfiguration<Entity> interface
                    var interfaceType = mappintConfigType.GetInterfaces().FirstOrDefault(z => z.Name.StartsWith("IEntityTypeConfiguration"));
                    if (interfaceType == null)
                    {
                        Console.WriteLine("interfaceType 为 null（不为 IEntityTypeConfiguration）");
                        continue;
                    }
                    //Legacy comment translated to English.
                    Type entityType = interfaceType.GenericTypeArguments[0];

                    //Legacy comment translated to English.
                    //if (ApplyedAutoConfigurationMappingTypes.Contains(entityType))
                    //{
                    //Legacy comment translated to English.
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
            }
            finally
            {
                SenparcTrace.SendCustomLog("监测到 ApplyAllAutoConfigurationMapping 执行", logs.ToString());
            }

            //Legacy comment translated to English.


        }
    }
}
