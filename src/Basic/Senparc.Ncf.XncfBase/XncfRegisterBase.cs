/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfRegisterBase.cs
    File description: XncfRegisterBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260804
    Change description: v0.24.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261108
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.29.1.2 Extend shared chat-tool contracts for admin task initiation.

----------------------------------------------------------------*/

using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Threads;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// Base implementation of the XNCF module registration contract.
    /// Derived registers provide identity metadata and may override lifecycle,
    /// database, MVC, AutoMapper, and MCP registration hooks.
    /// </summary>
    public abstract class XncfRegisterBase : IXncfRegister
    {
        /// <summary>
        /// Indicates whether the module should be excluded from installation workflows.
        /// </summary>
        public virtual bool IgnoreInstall { get; }

        /// <summary>
        /// Indicates whether the module exposes an MCP server registration.
        /// </summary>
        public virtual bool EnableMcpServer => false;

        /// <summary>Gets the module display name.</summary>
        public abstract string Name { get; }
        /// <summary>Gets the stable module UID.</summary>
        public abstract string Uid { get; }
        /// <summary>Gets the module version presented to installation and diagnostics.</summary>
        public abstract string Version { get; }
        /// <summary>Gets the administration menu name.</summary>
        public abstract string MenuName { get; }
        /// <summary>Gets the administration icon identifier.</summary>
        public abstract string Icon { get; }
        /// <summary>Gets the module description shown to administrators.</summary>
        public abstract string Description { get; }
        // The former Functions contract was removed from the public register API.
        // MCP and function discovery now use the module assembly and registered metadata.

        /// <summary>
        /// Gets or sets deferred AutoMapper profile registration actions.
        /// </summary>
        public virtual ConcurrentBag<Action<Profile>> AutoMapMappingConfigs { get; set; }
        /// <summary>
        /// Gets registered background threads whose names begin with this module's UID.
        /// </summary>
        public IEnumerable<KeyValuePair<ThreadInfo, Thread>> RegisteredThreadInfo => Register.ThreadCollection.Where(z => z.Value.Name.StartsWith(Uid));


        // Historical migration helpers were removed from the public register contract.
        // Database migration now runs through XncfDatabaseDbContext and module-specific
        // registration services.

        /// <summary>
        /// Provides the default no-op installation/update lifecycle hook.
        /// </summary>
        /// <param name="serviceProvider">The application service provider.</param>
        /// <param name="installOrUpdate">The requested installation operation.</param>
        /// <returns>A completed task unless a derived register performs asynchronous work.</returns>
        public virtual Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            return Task.CompletedTask;
        }
        /// <summary>
        /// Runs an uninstall callback supplied by the module host.
        /// </summary>
        /// <param name="serviceProvider">The application service provider.</param>
        /// <param name="unsinstallFunc">The asynchronous uninstall operation.</param>
        /// <returns>A task that completes after the callback.</returns>
        public virtual async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }

        /// <summary>
        /// Drops module entity tables and, for non-system modules, the module migration history table.
        /// Errors are logged as database exceptions so uninstall processing can continue across entities.
        /// </summary>
        /// <param name="serviceProvider">The application service provider.</param>
        /// <param name="databaseDbContext">The module database context.</param>
        /// <param name="entityType">The entity types whose tables should be dropped.</param>
        /// <returns>A task that completes after table-drop attempts finish.</returns>
        protected virtual async Task DropTablesAsync(IServiceProvider serviceProvider, DbContext databaseDbContext, Type[] entityType)
        {
            SenparcTrace.SendCustomLog("开始删除应用表格", MenuName + ", " + Name);
            var appliedMigrations = databaseDbContext.Database.GetAppliedMigrations();
            if (appliedMigrations.Count() > 0)
            {
                using (await databaseDbContext.Database.BeginTransactionAsync())
                {
                    //mySenparcEntities.Database.GetService<>
                }
                //var databaseCreator = mySenparcEntities.Database.GetService<IRelationalDatabaseCreator>();


                var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.Current;
                foreach (var type in entityType)
                {
                    try
                    {

                        var tableName = databaseDbContext.Model.FindEntityType(type).GetTableName();

                        #region Legacy section
                        //Legacy comment translated to English.
                        ////mySenparcEntities.Colors.FromSqlRaw($"DELETE FROM [{key}]");

                        ///string fullTableName = $"[{tableName}]";
                        ///if (!schma.IsNullOrEmpty())
                        ///{
                        ///    fullTableName = $"[{schma}].{fullTableName}";
                        ///}
                        #endregion

                        SenparcTrace.SendCustomLog("开始删除表格", $"tableName：{tableName}");

                        string dropTableSql = currentDatabaseConfiguration.GetDropTableSql(databaseDbContext, tableName);
                        if (!dropTableSql.IsNullOrEmpty())
                        {
                            int keyExeCount = await databaseDbContext.Database.ExecuteSqlRawAsync(dropTableSql);
                            SenparcTrace.SendCustomLog("影响行数", keyExeCount + " 行");
                        }
                        else
                        {
                            SenparcTrace.SendCustomLog("执行结果", $"{currentDatabaseConfiguration.GetType().Name} 内部已处理，无需单独执行 SQL");
                        }
                    }
                    catch (Exception ex)
                    {
                        SenparcTrace.BaseExceptionLog(new NcfDatabaseException(ex.Message, currentDatabaseConfiguration.GetType(), databaseDbContext.GetType(), ex));
                    }

                }

                //delete Migration record，ifforsystem tables，thennotdelete
                if (this is IXncfDatabase databaseRegister && databaseRegister.DatabaseUniquePrefix != NcfDatabaseMigrationHelper.SYSTEM_UNIQUE_PREFIX)
                {
                    var migrationHistoryTableName = NcfDatabaseMigrationHelper.GetDatabaseMigrationHistoryTableName(databaseRegister);
                    SenparcTrace.SendCustomLog("开始删除 DatabaseMigrationHistory 表格", $"[{migrationHistoryTableName}]");
                    string sqlStr = currentDatabaseConfiguration.GetDropTableSql(databaseDbContext, migrationHistoryTableName);
                    if (!sqlStr.IsNullOrEmpty())
                    {
                        int historyExeCount = await databaseDbContext.Database.ExecuteSqlRawAsync(sqlStr);
                        SenparcTrace.SendCustomLog("影响行数", historyExeCount + " 行");
                    }
                    else
                    {
                        SenparcTrace.SendCustomLog("执行结果", $"{currentDatabaseConfiguration.GetType().Name} 内部已处理，无需单独执行 SQL");
                    }

                }
            }
        }

        /// <summary>
        /// Returns the module area's configured home URL with the module UID appended.
        /// </summary>
        /// <returns>The module home URL, or <see langword="null"/> when this register is not an area register.</returns>
        public virtual string GetAreaHomeUrl()
        {
            if (this is IAreaRegister)
            {
                var homeUrl = (this as IAreaRegister).HomeUrl;
                return GetAreaUrl(homeUrl);
            }
            return null;
        }
        /// <summary>
        /// Appends this module's UID query parameter to an area URL.
        /// </summary>
        /// <param name="path">The relative or absolute area URL.</param>
        /// <returns>The URL with the module UID, <c>/</c> for a null area path, or <see langword="null"/> for non-area registers.</returns>
        public virtual string GetAreaUrl(string path)
        {
            if (this is IAreaRegister)
            {
                if (path == null)
                {
                    return "/";
                }

                path += path.Contains("?") ? "&" : "?";
                path += $"uid={Uid}";
                return path;
            }
            return null;
        }

        /// <summary>
        /// Registers module database contexts, database services, and shared XNCF database state.
        /// Derived modules may extend the returned service collection after this base setup.
        /// </summary>
        /// <param name="services">The dependency-injection service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="env">The host environment.</param>
        /// <returns>The configured service collection.</returns>
        public virtual IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            if (this is IXncfDatabase databaseRegister)
            {
                // Resolve and register every database context previously associated with this module.
                if (XncfDatabaseDbContextPool.Instance.ContainsKey(this.GetType()))
                {
                    var dbContextTypes = XncfDatabaseDbContextPool.Instance[this.GetType()];
                    foreach (var dbContextType in dbContextTypes.Values)
                    {
                        // Build the context through the configured database provider so the
                        // module receives the same connection and retry settings as the host.
                        Func<IServiceProvider, DbContext> implementationFactory = s =>
                        {
                            // DbContextOptions<TContext> is constructed reflectively because the
                            // registered context type is discovered at runtime.
                            var dbOptionBuilderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(dbContextType);
                            var dbOptionBuilder = Activator.CreateInstance(dbOptionBuilderType) as DbContextOptionsBuilder
                                ?? throw new InvalidOperationException($"无法创建数据库配置：{dbContextType.FullName}");

                            //getcurrentdatabaseconfiguration
                            var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.Current;

                            //usedatabase
                            currentDatabaseConfiguration.UseDatabase(dbOptionBuilder, Ncf.Core.Config.SenparcDatabaseConnectionConfigs.GetClientConnectionString(), new XncfDatabaseData(databaseRegister, null /*Legacy comment translated to English.*/), (b, xncfDatabaseData) =>
                                {
                                    // Module-specific options are applied by the database
                                    // configuration abstraction used by the host.

                                    currentDatabaseConfiguration.DbContextOptionsActionBase(b, xncfDatabaseData);

                                    //Legacy comment translated to English.
                                    //b.EnableRetryOnFailure(
                                    //    maxRetryCount: 5,
                                    //    maxRetryDelay: TimeSpan.FromSeconds(5),
                                    //    errorNumbersToAdd: new int[] { 2 });
                                });

                            // The helper supplies the correctly typed options instance to the
                            // dependency-injection container.
                            return CreateXncfDbContext(s, dbContextType, dbOptionBuilder.Options);
                        };
                        services.AddScoped(dbContextType, implementationFactory);

                        // Cache entity-set metadata used by repository and database helpers.
                        EntitySetKeys.TryLoadSetInfo(dbContextType);

                        if (!Ncf.Core.Config.SiteConfig.NcfCoreState.AynDatabaseXncfLoaded)
                        {
                            Ncf.Core.Config.SiteConfig.NcfCoreState.AynDatabaseXncfLoaded = true;
                        }

                    }
                }
                else
                {
                    var errMsg = $"{databaseRegister.GetType().FullName} 未注册任何数据库 DbContext！";
                    SenparcTrace.BaseExceptionLog(new NcfDatabaseException(errMsg, null, null));
                    Console.WriteLine(errMsg);
                }

                databaseRegister.AddXncfDatabaseModule(services);

                if (!Ncf.Core.Config.SiteConfig.NcfCoreState.AnyAddXncfDatabaseModuleApplied)
                {
                    Ncf.Core.Config.SiteConfig.NcfCoreState.AnyAddXncfDatabaseModuleApplied = true;
                }
            }

            return services;
        }

        /// <summary>
        /// Creates a module database context using aligned generic options for the requested context type.
        /// </summary>
        /// <param name="serviceProvider">The provider used to construct the context.</param>
        /// <param name="dbContextType">The concrete database context type.</param>
        /// <param name="dbContextOptions">The configured database options.</param>
        /// <returns>The created database context.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the context cannot be created.</exception>
        protected virtual DbContext CreateXncfDbContext(
            IServiceProvider serviceProvider,
            Type dbContextType,
            DbContextOptions dbContextOptions)
        {
            var options = AlignContextOptions(dbContextType, dbContextOptions);
            return ActivatorUtilities.CreateInstance(serviceProvider, dbContextType, options) as DbContext
                ?? throw new InvalidOperationException($"无法创建 XNCF 数据库上下文：{dbContextType.FullName}");
        }

        /// <summary>
        /// Aligns non-generic database options with the options type expected by a context constructor.
        /// </summary>
        /// <param name="dbContextType">The context type whose constructor should be inspected.</param>
        /// <param name="dbContextOptions">The source options.</param>
        /// <returns>The original options when compatible, or converted options when required.</returns>
        private static DbContextOptions AlignContextOptions(Type dbContextType, DbContextOptions dbContextOptions)
        {
            var optionsParameter = dbContextType.GetConstructors()
                .SelectMany(constructor => constructor.GetParameters())
                .FirstOrDefault(parameter => typeof(DbContextOptions).IsAssignableFrom(parameter.ParameterType));
            if (optionsParameter == null || optionsParameter.ParameterType.IsInstanceOfType(dbContextOptions))
            {
                return dbContextOptions;
            }

            var targetType = optionsParameter.ParameterType.IsGenericType
                ? optionsParameter.ParameterType
                : typeof(DbContextOptions<>).MakeGenericType(dbContextType);
            var extensions = dbContextOptions.Extensions.ToDictionary(
                extension => extension.GetType(),
                extension => (IDbContextOptionsExtension)extension);
            var aligned = Activator.CreateInstance(targetType, extensions) as DbContextOptions;
            if (aligned == null)
            {
                return dbContextOptions;
            }

            Console.WriteLine($"[NCF][DbContext] {dbContextType.FullName} 需要 {optionsParameter.ParameterType.Name}，已将 {dbContextOptions.GetType().FullName} 转换为 {aligned.GetType().FullName}。");
            return aligned;
        }

        /// <summary>
        /// Provides the default no-op module middleware hook.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="registerService">The NCF registration state.</param>
        /// <returns>The same application builder.</returns>
        public virtual IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            return app;
        }

        public static object AddAutoMapMappingLock = new object();
        /// <summary>
        /// Adds an AutoMapper profile configuration action to the module's deferred mapping collection.
        /// </summary>
        /// <param name="mapping">The profile configuration action.</param>
        public virtual void AddAutoMapMapping(Action<Profile> mapping)
        {
            if (AutoMapMappingConfigs == null)
            {
                lock (AddAutoMapMappingLock)
                {
                    if (AutoMapMappingConfigs == null)
                    {
                        AutoMapMappingConfigs = new ConcurrentBag<Action<Profile>>();
                    }
                }
            }
            AutoMapMappingConfigs.Add(mapping);
        }

        /// <summary>
        /// Provides the module AutoMapper hook. The base implementation performs no additional mapping.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        public virtual void OnAutoMapMapping(IServiceCollection services, IConfiguration configuration)
        {
            // Derived registers may add profiles or mapping conventions after service registration.
        }

        #region MCP

        /// <summary>
        /// Builds the internal MCP server name for this module.
        /// </summary>
        /// <returns>A stable server name derived from the module name.</returns>
        protected string GetMcpServerName()
        {
            return $"ncf-mcp-server-{this.Name.Replace(".", "-")}";
        }

        /// <summary>
        /// Gets loadable types from an assembly while retaining types that succeeded
        /// when reflection reports partial type-load failures.
        /// </summary>
        /// <param name="assembly">The assembly to inspect.</param>
        /// <returns>The loadable types.</returns>
        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null)!;
            }
        }

        /// <summary>
        /// Registers an MCP server and discovers MCP tool types from the module assembly.
        /// </summary>
        /// <param name="services">The dependency-injection service collection.</param>
        /// <param name="xncfRegister">The module register whose assembly should be scanned.</param>
        public virtual void AddMcpServer(IServiceCollection services, IXncfRegister xncfRegister)
        {
            var serverName = GetMcpServerName();
            try
            {
                var assembly = xncfRegister.GetType().Assembly;
                var hasMcpToolType = GetLoadableTypes(assembly)
                    .Any(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() != null);

                var mcpServerBuilder = services.AddMcpServer(opt =>
                {
                    opt.ServerInfo = new Implementation()
                    {
                        Name = serverName,
                        Version = this.Version,
                    };
                })
                .WithHttpTransport();

                if (hasMcpToolType)
                {
                    mcpServerBuilder.WithToolsFromAssembly(assembly);
                }
                else
                {
                    Console.WriteLine($"未检测到 MCP Tool Type，跳过工具注册（{this.Name}）");
                }

                XncfRegisterManager.McpServerInfoCollection[serverName] = new MCP.McpServerInfo()
                {
                    ServerName = serverName,
                    XncfName = Name,
                    XncfUid = Uid
                };
            }
            catch (Exception ex)
            {
                SenparcTrace.BaseExceptionLog(ex);
                Console.WriteLine($"MCP 服务注册失败（{this.Name}）：{ex.Message}");
            }
        }

        /// <summary>
        /// Maps the registered MCP server to the module-specific endpoint route.
        /// </summary>
        /// <param name="app">The application builder and endpoint route builder.</param>
        /// <param name="registerService">The NCF registration state.</param>
        public virtual void UseMcpServer(IApplicationBuilder app, IRegisterService registerService)
        {
            if (app is IEndpointRouteBuilder endpoints)
            {
                var serverName = GetMcpServerName();
                if (!XncfRegisterManager.McpServerInfoCollection.TryGetValue(serverName, out var mcpServerInfo))
                {
                    Console.WriteLine($"跳过 MCP 路由注册（{this.Name}）：未完成服务注册");
                    return;
                }

                var routePattern = $"mcp-{Name.Replace(".", "-").ToLower()}";
                try
                {
                    endpoints.MapMcp(routePattern);
                }
                catch (Exception ex)
                {
                    SenparcTrace.BaseExceptionLog(ex);
                    Console.WriteLine($"MCP 路由注册失败（{this.Name}）：{ex.Message}");
                    return;
                }

                // Store the resolved route so diagnostics and later consumers can discover it.
                mcpServerInfo.McpRoute = routePattern;

                Console.WriteLine($"启用 MCP 服务（{this.Name}）：{routePattern}");

            }
        }

        #endregion


        // The former public DbContextOptionsAction member was superseded by
        // DbContextOptionsActionBase and provider-specific configuration hooks.
        ///public Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsAction => (builder, xncfDatabaseData) =>
        ///{
        ///    if (xncfDatabaseData!=null && !xncfDatabaseData.AssemblyName.IsNullOrEmpty())
        ///    {

        ///    }
        ///};
    }
}
