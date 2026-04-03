using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
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
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// Base class for all XNCF module registrations
    /// </summary>
    public abstract class XncfRegisterBase : IXncfRegister
    {
        /// <summary>
        /// Whether to ignore installation (but does not affect registration code execution), default is false
        /// </summary>
        public virtual bool IgnoreInstall { get; }

        public virtual bool EnableMcpServer => false;

        /// <summary>
        /// Module name, must be globally unique
        /// </summary>
        public abstract string Name { get; }
        /// <summary>
        /// ID, must be globally unique
        /// </summary>
        public abstract string Uid { get; }
        /// <summary>
        /// Version number
        /// </summary>
        public abstract string Version { get; }
        /// <summary>
        /// Menu name
        /// </summary>
        public abstract string MenuName { get; }
        /// <summary>
        /// Icon
        /// </summary>
        public abstract string Icon { get; }
        /// <summary>
        /// Description
        /// </summary>
        public abstract string Description { get; }
        ///// <summary>
        ///// Registration method; the order of registration determines the display order in the UI
        ///// </summary>
        //public abstract IList<Type> Functions { get; }

        /// <summary>
        /// Add AutoMap mapping
        /// </summary>
        public virtual ConcurrentBag<Action<Profile>> AutoMapMappingConfigs { get; set; }
        /// <summary>
        /// Get the registered thread info for the current module
        /// </summary>
        public IEnumerable<KeyValuePair<ThreadInfo, Thread>> RegisteredThreadInfo => Register.ThreadCollection.Where(z => z.Value.Name.StartsWith(Uid));


        #region Execute Migrate to update data MigrateDatabaseAsync()
        /*
        /// <summary>
        /// Execute Migrate to update data
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="dbContextType"></param>
        /// <param name="checkdbContextType">Whether to check the dbContextType</param>
        /// <returns></returns>
        protected virtual async Task MigrateDatabaseAsync(IServiceProvider serviceProvider, Type dbContextType, bool checkdbContextType = true)
        {
            if (checkdbContextType && !dbContextType.IsSubclassOf(typeof(DbContext)))
            {
                throw new NcfDatabaseException("dbContextType parameter must inherit from DbContext", null, dbContextType);
            }

            var mySenparcEntities = serviceProvider.GetService(dbContextType) as DbContext;
            await mySenparcEntities.Database.MigrateAsync().ConfigureAwait(false);//Update database
        }


        /// <summary>
        /// Execute Migrate to update data
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        protected virtual async Task MigrateDatabaseAsync(IServiceProvider serviceProvider)
        {
            //The database context (DbContext) type corresponding to the current Register
            var dbContextType = MultipleDatabasePool.Instance.GetXncfDbContextType(this.GetType());
            await MigrateDatabaseAsync(serviceProvider, dbContextType, false);
        }

        /// <summary>
        /// Execute Migrate to update data
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
        /// Installation code
        /// </summary>
        public virtual Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            return Task.CompletedTask;
        }
        /// <summary>
        /// Uninstallation code
        /// </summary>
        public virtual async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }

        /// <summary>
        /// Delete table (use this method with caution!)
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="databaseDbContext"></param>
        /// <param name="entityType">The entity type corresponding to the table to be deleted</param>
        /// <returns></returns>
        protected virtual async Task DropTablesAsync(IServiceProvider serviceProvider, DbContext databaseDbContext, Type[] entityType)
        {
            SenparcTrace.SendCustomLog("Start deleting application tables", MenuName + ", " + Name);
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

                        #region Applicable to SQL Server
                        //SenparcTrace.SendCustomLog("Start deleting table", $"[schma].[tableName]: [{schma}].[{tableName}]");
                        ////mySenparcEntities.Colors.FromSqlRaw($"DELETE FROM [{key}]");

                        //string fullTableName = $"[{tableName}]";
                        //if (!schma.IsNullOrEmpty())
                        //{
                        //    fullTableName = $"[{schma}].{fullTableName}";
                        //}
                        #endregion

                        SenparcTrace.SendCustomLog("Start deleting table", $"tableName: {tableName}");

                        string dropTableSql = currentDatabaseConfiguration.GetDropTableSql(databaseDbContext, tableName);
                        if (!dropTableSql.IsNullOrEmpty())
                        {
                            int keyExeCount = await databaseDbContext.Database.ExecuteSqlRawAsync(dropTableSql);
                            SenparcTrace.SendCustomLog("Affected rows", keyExeCount + " rows");
                        }
                        else
                        {
                            SenparcTrace.SendCustomLog("Execution result", $"{currentDatabaseConfiguration.GetType().Name} handled internally, no need to execute SQL separately");
                        }
                    }
                    catch (Exception ex)
                    {
                        SenparcTrace.BaseExceptionLog(new NcfDatabaseException(ex.Message, currentDatabaseConfiguration.GetType(), databaseDbContext.GetType(), ex));
                    }

                }

                //Delete Migration records; system tables will not be deleted
                if (this is IXncfDatabase databaseRegister && databaseRegister.DatabaseUniquePrefix != NcfDatabaseMigrationHelper.SYSTEM_UNIQUE_PREFIX)
                {
                    var migrationHistoryTableName = NcfDatabaseMigrationHelper.GetDatabaseMigrationHistoryTableName(databaseRegister);
                    SenparcTrace.SendCustomLog("Start deleting DatabaseMigrationHistory table", $"[{migrationHistoryTableName}]");
                    string sqlStr = currentDatabaseConfiguration.GetDropTableSql(databaseDbContext, migrationHistoryTableName);
                    if (!sqlStr.IsNullOrEmpty())
                    {
                        int historyExeCount = await databaseDbContext.Database.ExecuteSqlRawAsync(sqlStr);
                        SenparcTrace.SendCustomLog("Affected rows", historyExeCount + " rows");
                    }
                    else
                    {
                        SenparcTrace.SendCustomLog("Execution result", $"{currentDatabaseConfiguration.GetType().Name} handled internally, no need to execute SQL separately");
                    }

                }
            }
        }

        /// <summary>
        /// Get the homepage URL
        /// <para>Only available for Registers that implement IAreaRegister; otherwise returns null</para>
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
        /// Get the URL of a specified page
        /// <para>Only available for Registers that implement IAreaRegister; otherwise returns null</para>
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
        /// Add module
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public virtual IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            if (this is IXncfDatabase databaseRegister)
            {
                //Iterate all databases in Register for registration
                if (XncfDatabaseDbContextPool.Instance.ContainsKey(this.GetType()))
                {
                    var dbContextTypes = XncfDatabaseDbContextPool.Instance[this.GetType()];
                    foreach (var dbContextType in dbContextTypes.Values)
                    {
                        var dbOptionBuilderType = dbContextType.GetConstructors()
                                            .First().GetParameters().First().ParameterType;

                        //Define XncfSenparcEntities instance generation
                        Func<IServiceProvider, DbContext> implementationFactory = s =>
                        {
                            DbContextOptionsBuilder dbOptionBuilder;
                            if (dbOptionBuilderType.GenericTypeArguments.Length > 0)
                            {
                                //With generics
                                ////Prepare to create DbContextOptionsBuilder instance, define type
                                dbOptionBuilderType = typeof(DbContextOptionsBuilder<>);
                                //dbOptionBuilderType = typeof(RelationalDbContextOptionsBuilder<,>);
                                //Get generic object type, e.g.: DbContextOptionsBuilder<SenparcEntities>
                                dbOptionBuilderType = dbOptionBuilderType.MakeGenericType(dbContextType);

                                //Create DbContextOptionsBuilder instance
                                dbOptionBuilder = Activator.CreateInstance(dbOptionBuilderType) as DbContextOptionsBuilder;
                            }
                            else
                            {
                                //Without generics
                                dbOptionBuilder = new DbContextOptionsBuilder();
                            }

                            //Get current database configuration
                            var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.Current;

                            //Use database
                            currentDatabaseConfiguration.UseDatabase(dbOptionBuilder, Ncf.Core.Config.SenparcDatabaseConnectionConfigs.GetClientConnectionString(), new XncfDatabaseData(databaseRegister, null /*use current Register assembly by default*/), (b, xncfDatabaseData) =>
                                {
                                    ////Perform additional configuration
                                    //this.DbContextOptionsAction?.Invoke(b);

                                    //Execute DbContextOptionsActionBase in DatabaseConfiguration for basic configuration;
                                    currentDatabaseConfiguration.DbContextOptionsActionBase(b, xncfDatabaseData);

                                    //Other configurations needed, e.g. for SQL Server:
                                    //b.EnableRetryOnFailure(
                                    //    maxRetryCount: 5,
                                    //    maxRetryDelay: TimeSpan.FromSeconds(5),
                                    //    errorNumbersToAdd: new int[] { 2 });
                                });

                            //Create SenparcEntities instance
                            var xncfSenparcEntities = Activator.CreateInstance(dbContextType, dbOptionBuilder.Options);
                            return xncfSenparcEntities as DbContext;
                        };
                        //Add XncfSenparcEntities dependency injection configuration
                        services.AddScoped(dbContextType, implementationFactory);

                        //Register current database object (required)
                        EntitySetKeys.TryLoadSetInfo(dbContextType);

                        //Any IxncfDatabase Register has completed execution
                        if (!Ncf.Core.Config.SiteConfig.NcfCoreState.AynDatabaseXncfLoaded)
                        {
                            Ncf.Core.Config.SiteConfig.NcfCoreState.AynDatabaseXncfLoaded = true;
                        }

                    }
                }
                else
                {
                    var errMsg = $"{databaseRegister.GetType().FullName} has not registered any database DbContext!";
                    SenparcTrace.BaseExceptionLog(new NcfDatabaseException(errMsg, null, null));
                    Console.WriteLine(errMsg);
                }

                //Add database-related registration process
                databaseRegister.AddXncfDatabaseModule(services);

                if (!Ncf.Core.Config.SiteConfig.NcfCoreState.AnyAddXncfDatabaseModuleApplied)
                {
                    //Any AddXncfDatabaseModule method has completed execution
                    Ncf.Core.Config.SiteConfig.NcfCoreState.AnyAddXncfDatabaseModuleApplied = true;
                }
            }

            return services;
        }

        /// <summary>
        /// Execute configuration in the Configure() method of startup.cs
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
        /// Execute AutoMapper mapping
        /// </summary>
        public virtual void OnAutoMapMapping(IServiceCollection services, IConfiguration configuration)
        {
            //Called in Register.StartEngine(), before AddXncfModule() method
        }

        #region MCP

        protected string GetMcpServerName()
        {
            return $"ncf-mcp-server-{this.Name.Replace(".", "-")}";
        }

        public virtual void AddMcpServer(IServiceCollection services, IXncfRegister xncfRegister)
        {
            var serverName = GetMcpServerName();

            var mcpServerBuilder = services.AddMcpServer(opt =>
            {
                opt.ServerInfo = new Implementation()
                {
                    Name = serverName,
                    Version = this.Version,
                };
            })
            .WithHttpTransport()
            .WithToolsFromAssembly(xncfRegister.GetType().Assembly);

            XncfRegisterManager.McpServerInfoCollection[serverName] = new MCP.McpServerInfo()
            {
                ServerName = serverName,
                XncfName = Name,
                XncfUid = Uid
            };
        }

        public virtual void UseMcpServer(IApplicationBuilder app, IRegisterService registerService)
        {
            if (app is IEndpointRouteBuilder endpoints)
            {
                var routePattern = $"mcp-{Name.Replace(".", "-").ToLower()}";
                endpoints.MapMcp(routePattern);

                //Register MCP route information
                var mcpServerInfo = XncfRegisterManager.McpServerInfoCollection.Values.LastOrDefault(z => z.XncfUid == Uid);
                if (mcpServerInfo == null)
                {
                    var serverName = GetMcpServerName();
                    mcpServerInfo = new MCP.McpServerInfo()
                    {
                        ServerName = serverName,
                        XncfName = Name,
                        XncfUid = Uid
                    };
                    XncfRegisterManager.McpServerInfoCollection[serverName] = mcpServerInfo;
                }

                mcpServerInfo.McpRoute = routePattern;

                //_logger.LogInformation($"Enable MCP service ({this.Name}): {routePattern}");

                Console.WriteLine($"Enable MCP service ({this.Name}): {routePattern}");

            }
        }

        #endregion


        ///// <summary>
        ///// Database DbContext options configuration (additional configuration)
        ///// <para>Parameter 1: IRelationalDbContextOptionsBuilderInfrastructure</para>
        ///// <para>Parameter 2: AssemblyName</para>
        ///// </summary>
        //public Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsAction => (builder, xncfDatabaseData) =>
        //{
        //    if (xncfDatabaseData!=null && !xncfDatabaseData.AssemblyName.IsNullOrEmpty())
        //    {

        //    }
        //};
    }
}
