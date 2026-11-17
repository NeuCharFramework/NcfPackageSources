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
    ///Legacy comment translated to English.
    /// </summary>
    public abstract class XncfRegisterBase : IXncfRegister
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public virtual bool IgnoreInstall { get; }

        public virtual bool EnableMcpServer => false;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public abstract string Name { get; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public abstract string Uid { get; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public abstract string Version { get; }
        /// <summary>
        /// menu name
        /// </summary>
        public abstract string MenuName { get; }
        /// <summary>
        /// Iconicon
        /// </summary>
        public abstract string Icon { get; }
        /// <summary>
        /// description
        /// </summary>
        public abstract string Description { get; }
        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        ///public abstract IList<Type> Functions { get; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public virtual ConcurrentBag<Action<Profile>> AutoMapMappingConfigs { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public IEnumerable<KeyValuePair<ThreadInfo, Thread>> RegisteredThreadInfo => Register.ThreadCollection.Where(z => z.Value.Name.StartsWith(Uid));


        #region Legacy section
        /*

        /// <summary>

        /// execute Migrate updatedata

        /// </summary>

        /// <param name="serviceProvider"></param>

        /// <param name="dbContextType"></param>

        Legacy comment translated to English.

        /// <returns></returns>

        protected virtual async Task MigrateDatabaseAsync(IServiceProvider serviceProvider, Type dbContextType, bool checkdbContextType = true)

        {

            if (checkdbContextType && !dbContextType.IsSubclassOf(typeof(DbContext)))

            {

                Legacy comment translated to English.

            }



            var mySenparcEntities = serviceProvider.GetService(dbContextType) as DbContext;

            await mySenparcEntities.Database.MigrateAsync().ConfigureAwait(false);//updatedatabase

        }





        /// <summary>

        /// execute Migrate updatedata

        /// </summary>

        /// <param name="serviceProvider"></param>

        /// <returns></returns>

        protected virtual async Task MigrateDatabaseAsync(IServiceProvider serviceProvider)

        {

            Legacy comment translated to English.

            var dbContextType = MultipleDatabasePool.Instance.GetXncfDbContextType(this.GetType());

            await MigrateDatabaseAsync(serviceProvider, dbContextType, false);

        }



        /// <summary>

        /// execute Migrate updatedata

        /// </summary>

        /// <typeparam name="TSenparcEntities"></typeparam>

        /// <param name="serviceProvider"></param>

        /// <returns></returns>

        protected virtual async Task MigrateDatabaseAsync<TSenparcEntities>(IServiceProvider serviceProvider)

            where TSenparcEntities : DbContext

        {

            await MigrateDatabaseAsync(serviceProvider, typeof(TSenparcEntities));

        }

        */
        #endregion

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public virtual Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            return Task.CompletedTask;
        }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public virtual async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="databaseDbContext"></param>
        ///Legacy comment translated to English.
        /// <returns></returns>
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
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
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
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
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
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public virtual IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            if (this is IXncfDatabase databaseRegister)
            {
                //Legacy comment translated to English.
                if (XncfDatabaseDbContextPool.Instance.ContainsKey(this.GetType()))
                {
                    var dbContextTypes = XncfDatabaseDbContextPool.Instance[this.GetType()];
                    foreach (var dbContextType in dbContextTypes.Values)
                    {
                        //Legacy comment translated to English.
                        Func<IServiceProvider, DbContext> implementationFactory = s =>
                        {
                            //Legacy comment translated to English.
                            //Legacy comment translated to English.
                            // DbContextOptions<TContext> constructor。
                            var dbOptionBuilderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(dbContextType);
                            var dbOptionBuilder = Activator.CreateInstance(dbOptionBuilderType) as DbContextOptionsBuilder
                                ?? throw new InvalidOperationException($"无法创建数据库配置：{dbContextType.FullName}");

                            //getcurrentdatabaseconfiguration
                            var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.Current;

                            //usedatabase
                            currentDatabaseConfiguration.UseDatabase(dbOptionBuilder, Ncf.Core.Config.SenparcDatabaseConnectionConfigs.GetClientConnectionString(), new XncfDatabaseData(databaseRegister, null /*Legacy comment translated to English.*/), (b, xncfDatabaseData) =>
                                {
                                    //Legacy comment translated to English.
                                    //this.DbContextOptionsAction?.Invoke(b);

                                    //Legacy comment translated to English.
                                    currentDatabaseConfiguration.DbContextOptionsActionBase(b, xncfDatabaseData);

                                    //Legacy comment translated to English.
                                    //b.EnableRetryOnFailure(
                                    //    maxRetryCount: 5,
                                    //    maxRetryDelay: TimeSpan.FromSeconds(5),
                                    //    errorNumbersToAdd: new int[] { 2 });
                                });

                            //Legacy comment translated to English.
                            //Legacy comment translated to English.
                            //Legacy comment translated to English.
                            return CreateXncfDbContext(s, dbContextType, dbOptionBuilder.Options);
                        };
                        //Legacy comment translated to English.
                        services.AddScoped(dbContextType, implementationFactory);

                        //Legacy comment translated to English.
                        EntitySetKeys.TryLoadSetInfo(dbContextType);

                        //Legacy comment translated to English.
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

                //Legacy comment translated to English.
                databaseRegister.AddXncfDatabaseModule(services);

                if (!Ncf.Core.Config.SiteConfig.NcfCoreState.AnyAddXncfDatabaseModuleApplied)
                {
                    //Legacy comment translated to English.
                    Ncf.Core.Config.SiteConfig.NcfCoreState.AnyAddXncfDatabaseModuleApplied = true;
                }
            }

            return services;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <remarks>
        ///Legacy comment translated to English.
        /// </remarks>
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
        ///Legacy comment translated to English.
        /// </summary>
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
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="registerService"></param>
        /// <returns></returns>
        public virtual IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            return app;
        }

        public static object AddAutoMapMappingLock = new object();
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
        ///Legacy comment translated to English.
        /// </summary>
        public virtual void OnAutoMapMapping(IServiceCollection services, IConfiguration configuration)
        {
            //Legacy comment translated to English.
        }

        #region MCP

        protected string GetMcpServerName()
        {
            return $"ncf-mcp-server-{this.Name.Replace(".", "-")}";
        }

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

                //Legacy comment translated to English.
                mcpServerInfo.McpRoute = routePattern;

                //Legacy comment translated to English.

                Console.WriteLine($"启用 MCP 服务（{this.Name}）：{routePattern}");

            }
        }

        #endregion


        ///// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///// </summary>
        ///public Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsAction => (builder, xncfDatabaseData) =>
        ///{
        ///    if (xncfDatabaseData!=null && !xncfDatabaseData.AssemblyName.IsNullOrEmpty())
        ///    {

        ///    }
        ///};
    }
}
