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
    /// Xncf Global Registration Class
    /// </summary>
    public static class Register
    {
        /// <summary>
        /// Collection of all threads
        /// </summary>
        public static ConcurrentDictionary<ThreadInfo, Thread> ThreadCollection { get; set; } = new ConcurrentDictionary<ThreadInfo, Thread>();

        public static FunctionRenderCollection FunctionRenderCollection { get; set; } = new FunctionRenderCollection();

        /// <summary>
        /// ConfigurationMapping objects for all automatically registered Xncf databases
        /// </summary>
        public static List<object> XncfAutoConfigurationMappingList { get; set; } = new List<object>();
        //public static List<IEntityTypeConfiguration<EntityBase>> XncfAutoConfigurationMappingList = new List<IEntityTypeConfiguration<EntityBase>>();

        /// <summary>
        /// Assembly scan classification
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
        ///// Start the XNCF module engine, including initialization scanning and registration
        ///// </summary>
        ///// <returns></returns>
        //[Obsolete("Please use StartNcfEngine()", true)]
        //public static string StartEngine(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        //{
        //    return StartNcfEngine(services, configuration, env, null);
        //}

        /// <summary>
        /// Start the XNCF module engine, including initialization scanning and registration
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="env"></param>
        /// <param name="dllFilePatterns">File names of included dlls; ".Xncf." will always be included</param>
        /// <returns></returns>
        public static string StartNcfEngine(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env, string[] dllFilePatterns)
        {
            StringBuilder sb = new StringBuilder();
            SetLog(sb, "Start scanning XncfModules");

            Senparc.Ncf.Core.VersionManager.ShowSuccessTip($"\tPre-start self-check begins {SystemTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}", null, false);

            var scanTypesCount = 0;
            var hideTypeCount = 0;
            ConcurrentDictionary<Type, ScanTypeKind> types = new ConcurrentDictionary<Type, ScanTypeKind>();

            //All XNCF modules, including ignored ones.
            //var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();
            //using (cache.BeginCacheLock("Senparc.Ncf.XncfBase.Register", "Scan")) //Cache configuration not yet complete during registration phase
            {
                try
                {
                    //Iterate over all assemblies
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
                            return;//Ignore AutoMapper
                        }

                        scanTypesCount++;
                        var aTypes = a.GetTypes();

                        List<Exception> exceptions = new List<Exception>();

                        //Iterate over all types in the assembly
                        foreach (var t in aTypes)
                        {
                            try
                            {
                                if (t.IsAbstract)
                                {
                                    continue;//Ignore abstract classes
                                }

                                //Console.WriteLine(t.GetType().Name);
                                //Get XncfRegister
                                if (t.GetInterfaces().Contains(typeof(IXncfRegister)))
                                {
                                    types[t] = ScanTypeKind.IXncfRegister;
                                }
                                //Get XncfFunction
                                //if (t.GetInterfaces().Contains(typeof(IXncfFunction)))
                                //{
                                //    types[t] = ScanTypeKind.IXncfFunction; /* Not collected for processing temporarily */
                                //}
                                //Get XncfAutoConfigurationMapping
                                if (t.GetCustomAttributes(true).FirstOrDefault(z => z is XncfAutoConfigurationMappingAttribute) != null
                                    /*&& t.GetInterfaces().Contains(typeof(IEntityTypeConfiguration<>))*/)
                                {
                                    types[t] = ScanTypeKind.XncfAutoConfigurationMappingAttribute;
                                }


                                //Get multi-database configuration (subclasses of XncfDatabaseDbContext)
                                if (t.IsSubclassOf(typeof(DbContext)) /*t.IsSubclassOf(typeof(XncfDatabaseDbContext))*/ &&
                                    t.GetInterface(nameof(ISenparcEntitiesDbContext)) != null &&
                                    t.GetCustomAttributes(true).FirstOrDefault(z => z is MultipleMigrationDbContextAttribute) != null)
                                {
                                    //Get attribute
                                    var multiDbContextAttr = t.GetCustomAttributes(true).FirstOrDefault(z => z is MultipleMigrationDbContextAttribute) as MultipleMigrationDbContextAttribute;

                                    //Add configuration
                                    var multipleDatabasePool = MultipleDatabasePool.Instance;
                                    var result = multipleDatabasePool.TryAdd(multiDbContextAttr, t, new[] { columnWidth1, columnWidth2, columnWidth3 });
                                    SetLog(sb, result, false);
                                }

                                //Configure FunctionRender
                                if (t.IsSubclassOf(typeof(AppServiceBase)))
                                {
                                    //Iterate over specific methods
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

                                //Configure ServiceBase
                                if (
                                    !t.IsAbstract && (

                                    //Since generics are determined at compile time, IsSubclassOf cannot be used directly to check if a class is a subclass of a generic base class. Use GetGenericTypeDefinition to get the generic base class for comparison.
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
                                SenparcTrace.SendCustomLog("Error occurred while scanning assembly", $"Type:{t.FullName}");
                            }
                        }

                        if (exceptions.Count > 0)
                        {
                            var errMsg = $"Assembly [{a.FullName}] detected {exceptions.Count} exceptions; non-core assemblies can be ignored";
                            Console.WriteLine(errMsg);
                            SenparcTrace.SendCustomLog("Assembly scan exception log", errMsg);
                        }

                    }, true);

                    SetLog(sb, $"{new String('-', columnWidth1 + columnWidth2 + columnWidth3 + 6)}", false);
                    SetLog(sb, "");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Assembly scan exited abnormally; complete assembly information may not be available: {ex.Message}");
                }

                SetLog(sb, $"Satisfies the ScanTypeKind condition: {types.Count()}");

                //Register XncfRegister first
                {
                    //Filter
                    var allTypes = types.Where(z => z.Value == ScanTypeKind.IXncfRegister/* && z.Key.GetInterfaces().Contains(typeof(IXncfRegister))*/)
                        .Select(z => z.Key);
                    //Sort by priority in descending order (higher number = higher priority)
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
                                    Console.WriteLine($"MultipleDatabaseType is null (polling ${type.FullName})");
                                }
                                else if (multipleDatabaseType == MultipleDatabaseType.InMemory)
                                {
                                    //Unit test mode, ignore duplicate loading
                                    Console.WriteLine("MultipleDatabaseType.InMemory mode, unit test ignores duplicate loading");
                                }
                                else
                                {
                                    throw new XncfFunctionException("A module with the same Uid already exists: " + register.Uid);
                                }
                            }

                            if (register.IgnoreInstall)
                            {
                                hideTypeCount++;
                            }
                            XncfRegisterManager.RegisterList.Add(register);//Only register if installation is allowed; otherwise execution ends
                            services.AddScoped(type);//Register in DI
                                                     //foreach (var functionType in register.Functions)
                                                     //{
                                                     //    services.AddScoped(functionType);//Register in DI
                                                     //}
                        }

                        //Initialize database
                        if (register is IXncfDatabase)
                        {

                        }

                    }
                }

                //Handle XncfAutoConfigurationMappingAttribute
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

            //Multi-tenant
            services.AddScoped<RequestTenantInfo>();//TODO: needs dynamic recognition; read and convert from current request cache

            //Register services in Senarc.Ncf.Service
            services.AddScoped<SysButtonService>();
            services.AddScoped<SysMenuService>();
            services.AddScoped<SysRoleAdminUserInfoService>();
            services.AddScoped<SysRolePermissionService>();
            services.AddScoped<SysRoleService>();

            services.ScanAssamblesForAutoDI();
            //All assembly auto-scan delegates have been added; run the scan immediately (required)
            AssembleScanHelper.RunScan(dllFilePatterns);
            //services.AddSingleton<Core.Cache.RedisProvider.IRedisProvider, Core.Cache.RedisProvider.StackExchangeRedisProvider>();


            #region Support AutoMapper

            //Console.WriteLine("----------");
            //Console.WriteLine("XNCF modules performing AutoMapper mapping foreach (var xncfRegister in XncfRegisterManager.RegisterList)");
            //Console.WriteLine("----------");
            //XNCF modules perform AutoMapper mapping
            foreach (var xncfRegister in XncfRegisterManager.RegisterList)
            {
                xncfRegister.OnAutoMapMapping(services, configuration);
            }

            //Console.WriteLine("----------");
            //Console.WriteLine("Include current system");
            //Console.WriteLine("----------");

            //Include current system
            services.AddAutoMapper(z => z.AddProfile<Core.AutoMapper.SystemProfile>());

            //Include all modules    TODO: XncfModuleProfile constructor will execute after "XNCF modules AutoMapper mapping", making XNCF module AutoMapper mapping ineffective
            services.AddAutoMapper(z => z.AddProfile<AutoMapper.XncfModuleProfile>());

            #endregion

            //Note: AutoMapper must be placed before XNCF registration, because dynamic assemblies inside XNCF may cause exceptions

            //XNCF modules perform Service registration
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
                    _ = new NcfExceptionBase($"{xncfRegister.Name} module xncfRegister.AddXncfModule() error: {ex.Message}", ex);

                }
            }

            //Mark all AddXncfModule methods as having completed execution
            Ncf.Core.Config.SiteConfig.NcfCoreState.AllAddXncfModuleApplied = true;
            Ncf.Core.Config.SiteConfig.NcfCoreState.AllDatabaseXncfLoaded = true;

            SetLog(sb, $"Finish services.AddXncfModule(): Total of {scanTypesCount} assemblies were scanned.");

            return sb.ToString();
        }

        /// <summary>
        /// Scan and install (auto-install, no manual action required)
        /// TODO: Move to Service for automatic system module upgrades
        /// </summary>
        /// <param name="xncfModuleDtos">Currently installed modules</param>
        /// <param name="serviceProvider">IServiceProvider</param>
        /// <param name="afterInstalledOrUpdated">Callback executed after install or update</param>
        /// <param name="justScanThisUid">Only scan and update a specific Uid</param>
        /// <returns></returns>
        public static async Task<string> ScanAndInstall(IList<CreateOrUpdate_XncfModuleDto> xncfModuleDtos,
            IServiceProvider serviceProvider,
            Func<IXncfRegister, InstallOrUpdate, Task> afterInstalledOrUpdated = null,
            string justScanThisUid = null)
        {
            StringBuilder sb = new StringBuilder();
            SetLog(sb, "Start scanning XncfModules");

            //Register first
            var updatedCount = 0;
            var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();
            using (await cache.BeginCacheLockAsync("Senparc.Ncf.XncfBase.Register", "Scan").ConfigureAwait(false))
            {
                foreach (var register in XncfRegisterManager.RegisterList)
                {
                    SetLog(sb, $"Found IXncfRegister: {register.GetType().FullName}");
                    if (register.IgnoreInstall)
                    {
                        SetLog(sb, $"Current module requires installation to be ignored, uid: [{justScanThisUid}], skipping this module");
                        continue;
                    }

                    if (justScanThisUid != null && register.Uid != justScanThisUid)
                    {
                        SetLog(sb, $"Only updating uid: [{justScanThisUid}], skipping this module");
                        continue;
                    }
                    else
                    {
                        SetLog(sb, "Meets install/update requirements, proceeding");
                    }

                    var xncfModuleStoredDto = xncfModuleDtos.FirstOrDefault(z => z.Uid == register.Uid);
                    var xncfModuleAssemblyDto = new UpdateVersion_XncfModuleDto(register.Name, register.Uid, register.MenuName, register.Version, register.Description, register.Icon);

                    //Check for updates and install to database
                    var xncfModuleService = serviceProvider.GetService<XncfModuleService>();
                    var installOrUpdate = await xncfModuleService.CheckAndUpdateVersionAsync(xncfModuleStoredDto, xncfModuleAssemblyDto).ConfigureAwait(false);
                    SetLog(sb, $"Version update status: {installOrUpdate?.ToString() ?? "Not installed"}");

                    if (installOrUpdate.HasValue)
                    {
                        updatedCount++;

                        //Execute install routine
                        await register.InstallOrUpdateAsync(serviceProvider, installOrUpdate.Value).ConfigureAwait(false);

                        await afterInstalledOrUpdated?.Invoke(register, installOrUpdate.Value);
                    }
                }
            }

            SetLog(sb, $"Scan complete, {updatedCount} assemblies added or updated");
            return sb.ToString();
        }

        ///// <summary>
        ///// Typically executed in the Configure() method of Startup.cs
        ///// </summary>
        ///// <param name="app"></param>
        ///// <param name="registerService">CO2NET registration object</param>
        ///// <param name="senparcCoreSetting">SenparcCoreSetting</param>
        ///// <returns></returns>
        //public static IApplicationBuilder UseXncfModules<TDatabaseConfiguration>(this IApplicationBuilder app, IRegisterService registerService, SenparcCoreSetting senparcCoreSetting = null, bool autoRunInstall = false)
        //where TDatabaseConfiguration : IDatabaseConfiguration, new()
        //{
        //    return UseXncfModules<TDatabaseConfiguration>(app, registerService, senparcCoreSetting, autoRunInstall);
        //}


        /// <summary>
        /// Typically executed in the Configure() method of Startup.cs
        /// </summary>
        /// <param name="app"></param>
        /// <param name="registerService">CO2NET registration object</param>
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

                    //TODO: later change to remote (other module) tenant lookup

                    // Whether a database-capable module has already been loaded
                    if (!SiteConfig.DatabaseXncfLoaded
                        //SystemCore needs to support IXncfDatabase, but is not an actual database module, so it should be excluded
                        && register.Uid != SiteConfig.SYSTEM_XNCF_MODULE_SYSTEM_CORE_UID
                        && register is IXncfDatabase
                        )
                    {
                        SiteConfig.DatabaseXncfLoaded = true;
                    }
                }
                catch (Exception ex)
                {
                    SenparcTrace.SendCustomLog("Exception occurred executing register.UseXncfModule() from XncfRegisterManager.RegisterList",
                        $@"Module: {register.Name}
Exception: {ex.Message}
Details: {ex.ToString()}");
                }

                //Execute middleware
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

                //Execute threads
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

                    //Any one ThreadXncf has been loaded
                    Ncf.Core.Config.SiteConfig.NcfCoreState.AnyThreadXncfLoaded = true;
                }


                //MCP server
                if (register.EnableMcpServer)
                {
                    register.UseMcpServer(app, registerService);
                }
            }

            //All ThreadXncf have been loaded
            Ncf.Core.Config.SiteConfig.NcfCoreState.AllThreadXncfLoaded = true;
            //All UseXncfModule methods have completed
            Ncf.Core.Config.SiteConfig.NcfCoreState.AllUseXncfModuleApplied = true;

            //Auto-run installation

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
        /// All entity types corresponding to [AutoConfigurationMapping] that have been applied
        /// </summary>
        public static List<Type> ApplyedAutoConfigurationMappingTypes { get; set; } = new List<Type>();
        private static List<Type> AddedApplyedAutoConfigurationMappingEntityTypes { get; set; } = new List<Type>();
        /// <summary>
        /// Automatically add all objects marked with [XncfAutoConfigurationMapping] attribute in XNCF modules
        /// </summary>
        public static void ApplyAllAutoConfigurationMapping(ModelBuilder modelBuilder)
        {
            var entityTypeConfigurationMethod = typeof(ModelBuilder).GetMethods()
                .FirstOrDefault(z => z.Name == "ApplyConfiguration" && z.ContainsGenericParameters && z.GetParameters().SingleOrDefault()?.ParameterType.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));

            //Automatically retrieve all entity types under DbSet from all module database entities

            //TODO: Filter base class types
            var xncfDatabaseDbContextList = MultipleDatabasePool.Instance.Values.SelectMany(z => z.Values);
            //TODO: To further improve efficiency, deduplicate subclasses of the same DbContext

            foreach (var xncfDatabaseDbContextType in xncfDatabaseDbContextList)
            {
                var setKeyInfoList = EntitySetKeys.GetEntitySetInfo(xncfDatabaseDbContextType).Values;
                foreach (var setKeyInfo in setKeyInfoList)
                {
                    //Database entity type
                    var entityType = setKeyInfo.DbSetType;
                    if (AddedApplyedAutoConfigurationMappingEntityTypes.Contains(entityType))
                    {
                        //Console.WriteLine($"\t [{xncfDatabaseDbContextType.Name}]ApplyAllAutoConfigurationMapping has duplicate setKeyInfo：{entityType.Name}, skipped");
                        continue;
                    }
                    //Console.WriteLine($"\t [{xncfDatabaseDbContextType.Name}]ApplyAllAutoConfigurationMapping processing setKeyInfo：{entityType.Name}");

                    //Default empty ConfigurationMapping object generic type
                    var blankEntityTypeConfigurationType = typeof(BlankEntityTypeConfiguration<>).MakeGenericType(entityType);
                    //Create a new instance
                    var blankEntityTypeConfiguration = Activator.CreateInstance(blankEntityTypeConfigurationType);
                    //Append to end so user-defined code executes with higher priority
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

                    //Get configration entity type, e.g.: DbConfig_WeixinUserConfigurationMapping
                    Type mappintConfigType = autoConfigurationMapping.GetType();
                    //Get IEntityTypeConfiguration<Entity> interface
                    var interfaceType = mappintConfigType.GetInterfaces().FirstOrDefault(z => z.Name.StartsWith("IEntityTypeConfiguration"));
                    if (interfaceType == null)
                    {
                        Console.WriteLine("interfaceType is null (not IEntityTypeConfiguration)");
                        continue;
                    }
                    //Entity type, e.g.: DbConfig
                    Type entityType = interfaceType.GenericTypeArguments[0];

                    //PS: Cannot filter here, otherwise only the first registered object in e.g. SystemServiceEntities_SqlServer / SystemServiceEntities_Mysql succeeds, later ones are ignored
                    //if (ApplyedAutoConfigurationMappingTypes.Contains(entityType))
                    //{
                    //    //Skip if already added. This check is needed because original XncfAutoConfigurationMappingList data may overlap with auto-added DataSet objects
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
                SenparcTrace.SendCustomLog("ApplyAllAutoConfigurationMapping execution detected", logs.ToString());
            }

            //TODO: Add IQueryTypeConfiguration<>


        }
    }
}
