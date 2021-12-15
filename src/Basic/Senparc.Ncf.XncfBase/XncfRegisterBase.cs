using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    /// 所有 XNCF 模块注册的基类
    /// </summary>
    public abstract class XncfRegisterBase : IXncfRegister
    {
        /// <summary>
        /// 是否忽略安装（但不影响执行注册代码），默认为 false
        /// </summary>
        public virtual bool IgnoreInstall { get; }

        /// <summary>
        /// 模块名称，要求全局唯一
        /// </summary>
        public abstract string Name { get; }
        /// <summary>
        /// 编号，要求全局唯一
        /// </summary>
        public abstract string Uid { get; }
        /// <summary>
        /// 版本号
        /// </summary>
        public abstract string Version { get; }
        /// <summary>
        /// 菜单名称
        /// </summary>
        public abstract string MenuName { get; }
        /// <summary>
        /// Icon图标
        /// </summary>
        public abstract string Icon { get; }
        /// <summary>
        /// 说明
        /// </summary>
        public abstract string Description { get; }
        ///// <summary>
        ///// 注册方法，注册的顺序决定了界面中排列的顺序
        ///// </summary>
        //public abstract IList<Type> Functions { get; }

        /// <summary>
        /// 添加 AutoMap 映射
        /// </summary>
        public virtual ConcurrentBag<Action<Profile>> AutoMapMappingConfigs { get; set; }
        /// <summary>
        /// 获取当前模块的已注册线程信息
        /// </summary>
        public IEnumerable<KeyValuePair<ThreadInfo, Thread>> RegisteredThreadInfo => Register.ThreadCollection.Where(z => z.Value.Name.StartsWith(Uid));



        #region 执行 Migrate 更新数据 MigrateDatabaseAsync()
        /*
        /// <summary>
        /// 执行 Migrate 更新数据
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="dbContextType"></param>
        /// <param name="checkdbContextType">是否需要对 dbContextType 类型进行检查</param>
        /// <returns></returns>
        protected virtual async Task MigrateDatabaseAsync(IServiceProvider serviceProvider, Type dbContextType, bool checkdbContextType = true)
        {
            if (checkdbContextType && !dbContextType.IsSubclassOf(typeof(DbContext)))
            {
                throw new NcfDatabaseException("dbContextType 参数必须继承自 DbContext", null, dbContextType);
            }

            var mySenparcEntities = serviceProvider.GetService(dbContextType) as DbContext;
            await mySenparcEntities.Database.MigrateAsync().ConfigureAwait(false);//更新数据库
        }


        /// <summary>
        /// 执行 Migrate 更新数据
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        protected virtual async Task MigrateDatabaseAsync(IServiceProvider serviceProvider)
        {
            //当前Register对应的数据库上下文（DbContext）类型
            var dbContextType = MultipleDatabasePool.Instance.GetXncfDbContextType(this.GetType());
            await MigrateDatabaseAsync(serviceProvider, dbContextType, false);
        }

        /// <summary>
        /// 执行 Migrate 更新数据
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
        /// 安装代码
        /// </summary>
        public virtual Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            return Task.CompletedTask;
        }
        /// <summary>
        /// 卸载代码
        /// </summary>
        public virtual async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }

        /// <summary>
        /// 删除表（此方法请慎重使用！）
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="databaseDbContext"></param>
        /// <param name="entityType">需要删除的表所对应的实体类型</param>
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

                        #region 适用于 SQL Server
                        //SenparcTrace.SendCustomLog("开始删除表格", $"[schma].[tableName]：[{schma}].[{tableName}]");
                        ////mySenparcEntities.Colors.FromSqlRaw($"DELETE FROM [{key}]");

                        //string fullTableName = $"[{tableName}]";
                        //if (!schma.IsNullOrEmpty())
                        //{
                        //    fullTableName = $"[{schma}].{fullTableName}";
                        //}
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

                //删除 Migration 记录，如果为系统表，则不删除
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
        /// 获取首页Url
        /// <para>仅限实现了 IAreaRegister 接口之后的 Register，否则将返回 null</para>
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
        /// 获取指定网页的Url
        /// <para>仅限实现了 IAreaRegister 接口之后的 Register，否则将返回 null</para>
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
        /// 添加模块
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public virtual IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            if (this is IXncfDatabase databaseRegister)
            {

                //遍历所有Register中的数据库进行注册
                if (XncfDatabaseDbContextPool.Instance.ContainsKey(this.GetType()))
                {
                    var dbContextTypes = XncfDatabaseDbContextPool.Instance[this.GetType()];
                    foreach (var dbContextType in dbContextTypes.Values)
                    {
                        var dbOptionBuilderType = dbContextType.GetConstructors()
                                            .First().GetParameters().First().ParameterType;

                        //定义 XncfSenparcEntities 实例生成
                        Func<IServiceProvider, DbContext> implementationFactory = s =>
                        {
                            DbContextOptionsBuilder dbOptionBuilder;
                            if (dbOptionBuilderType.GenericTypeArguments.Length > 0)
                            {
                                //带泛型
                                ////准备创建 DbContextOptionsBuilder 实例，定义类型
                                dbOptionBuilderType = typeof(DbContextOptionsBuilder<>);
                                //dbOptionBuilderType = typeof(RelationalDbContextOptionsBuilder<,>);
                                //获取泛型对象类型，如：DbContextOptionsBuilder<SenparcEntities>
                                dbOptionBuilderType = dbOptionBuilderType.MakeGenericType(dbContextType);

                                //创建 DbContextOptionsBuilder 实例
                                dbOptionBuilder = Activator.CreateInstance(dbOptionBuilderType) as DbContextOptionsBuilder;
                            }
                            else
                            {
                                //不带泛型
                                dbOptionBuilder = new DbContextOptionsBuilder();
                            }

                            //获取当前数据库配置
                            var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.Current;

                            //使用数据库
                            currentDatabaseConfiguration.UseDatabase(dbOptionBuilder, Ncf.Core.Config.SenparcDatabaseConnectionConfigs.ClientConnectionString, new XncfDatabaseData(databaseRegister, null /*默认使用当前 Register 程序集*/), (b, xncfDatabaseData) =>
                                {
                                    ////进行附加配置
                                    //this.DbContextOptionsAction?.Invoke(b);

                                    //执行 DatabaseConfiguration 中的 DbContextOptionsActionBase，进行基础配置;
                                    currentDatabaseConfiguration.DbContextOptionsActionBase(b, xncfDatabaseData);

                                    //其他需要进行的配置，如对于 SQL Server：
                                    //b.EnableRetryOnFailure(
                                    //    maxRetryCount: 5,
                                    //    maxRetryDelay: TimeSpan.FromSeconds(5),
                                    //    errorNumbersToAdd: new int[] { 2 });
                                });

                            //创建 SenparcEntities 实例
                            var xncfSenparcEntities = Activator.CreateInstance(dbContextType, dbOptionBuilder.Options);
                            return xncfSenparcEntities as DbContext;
                        };
                        //添加 XncfSenparcEntities 依赖注入配置
                        services.AddScoped(dbContextType, implementationFactory);

                        //注册当前数据库的对象（必须）
                        EntitySetKeys.TryLoadSetInfo(dbContextType);
                    }
                }
                else
                {
                    var errMsg = $"{databaseRegister.GetType().FullName} 未注册任何数据库 DbContext！";
                    SenparcTrace.BaseExceptionLog(new NcfDatabaseException(errMsg, null, null));
                    Console.WriteLine(errMsg);
                }

                //添加数据库相关注册过程
                databaseRegister.AddXncfDatabaseModule(services);
            }
            return services;
        }

        /// <summary>
        /// 在 startup.cs 的 Configure() 方法中执行配置
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
        /// 执行 AutoMapper 映射 
        /// </summary>
        public virtual void OnAutoMapMapping(IServiceCollection services, IConfiguration configuration)
        {
            //在 Register.StartEngine() 中调用，早于 AddXncfModule() 方法
        }


        ///// <summary>
        ///// 数据库 DbContext 选项配置（附加配置）
        ///// <para>第1个参数：IRelationalDbContextOptionsBuilderInfrastructure</para>
        ///// <para>第2个参数：AssemblyName</para>
        ///// </summary>
        //public Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsAction => (builder, xncfDatabaseData) =>
        //{
        //    if (xncfDatabaseData!=null && !xncfDatabaseData.AssemblyName.IsNullOrEmpty())
        //    {

        //    }
        //};
    }
}
