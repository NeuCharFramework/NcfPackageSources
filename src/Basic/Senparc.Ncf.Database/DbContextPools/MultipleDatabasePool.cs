using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Ncf.Database
{
    public class XncfDatabaseDbContextWapper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="migrationDbContextType">设计时（或运行时进行 Database.Migrate() 操作的）所使用的 XncfDatabaseDbContext 类型</param>
        /// <param name="runtimeDbContextType">运行时所使用的 XncfDatabaseDbContext 类型（通常为进行查询时）</param>
        public XncfDatabaseDbContextWapper(Type migrationDbContextType, Type runtimeDbContextType)
        {
            MigrationDbContextType = migrationDbContextType;
            RuntimeDbContextType = runtimeDbContextType;
        }

        public Type MigrationDbContextType { get; set; }
        public Type RuntimeDbContextType { get; set; }
    }

    /// <summary>
    /// 多数据库配置池
    /// <para>Value 为 Dictionary<Type/* IXncfDatabase Register 类型*/, Type/* 数据库 XncfDatabaseDbContext 类型 */></para>
    /// </summary>
    public class MultipleDatabasePool
        : Dictionary<MultipleDatabaseType, Dictionary<Type/* IXncfDatabase Register 类型*/, Type/* 数据库 XncfDatabaseDbContext 类型 */>>
    {
        #region 单例

        MultipleDatabasePool() { }

        /// <summary>
        /// DatabaseConfigurationFactory 的全局单例
        /// </summary>
        public static MultipleDatabasePool Instance
        {
            get
            {
                return Nested.instance;
            }
        }

        class Nested
        {
            static Nested() { }

            internal static readonly MultipleDatabasePool instance = new MultipleDatabasePool();
        }

        #endregion

        /// <summary>
        /// 添加配置
        /// </summary>
        /// <param name="multiDbContextAttr"></param>
        /// <param name="xncfDatabaseDbContextType">实现了 IXncfDatabase 接口的类型</param>
        /// <param name="logColumnWidth">输出日志表格的宽度</param>
        /// <returns></returns>
        public string TryAdd(MultipleMigrationDbContextAttribute multiDbContextAttr, Type xncfDatabaseDbContextType, int[] logColumnWidth)
        {
            var msg = $"| {multiDbContextAttr.XncfDatabaseRegisterType.FullName.PadRight(logColumnWidth[0])}| {xncfDatabaseDbContextType.Name.PadRight(logColumnWidth[1])}| {multiDbContextAttr.MultipleDatabaseType.ToString().PadRight(logColumnWidth[2])}";

            //查看是否已经包含 MultipleDatabaseType 
            if (!this.ContainsKey(multiDbContextAttr.MultipleDatabaseType))
            {
                //添加 MultipleDatabaseType 对应集合
                this[multiDbContextAttr.MultipleDatabaseType] = new Dictionary<Type, Type>();
            }

            //加入配置
            this[multiDbContextAttr.MultipleDatabaseType][multiDbContextAttr.XncfDatabaseRegisterType] = xncfDatabaseDbContextType;

            //同步添加到 XncfDatabaseDbContextPool
            XncfDatabaseDbContextPool.Instance.TryAdd(multiDbContextAttr, xncfDatabaseDbContextType);

            return msg;
        }

        /// <summary>
        /// 获取指定 IXncfDatabase 关联的当前数据库上下文（DbContext）
        /// </summary>
        /// <param name="xncfDatabaseRegister">实现了 IXncfDatabase 的实体</param>
        /// <returns></returns>
        public Type GetXncfDbContextType(IXncfDatabase xncfDatabaseRegister)
        {
            return GetXncfDbContextType(xncfDatabaseRegister.GetType());
        }

        /// <summary>
        /// 获取指定 IXncfDatabase 关联的当前数据库上下文（DbContext）
        /// </summary>
        /// <param name="xncfDatabaseRegisterType">实现了 IXncfDatabase 的具体类型</param>
        /// <returns></returns>
        public Type GetXncfDbContextType(Type xncfDatabaseRegisterType)
        {
            //数据库配置工厂
            var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
            //当前数据库配置
            var currentDatabaseConfiguration = databaseConfigurationFactory.Current;
            //当前数据库类型
            MultipleDatabaseType multipleDatabaseType = currentDatabaseConfiguration.MultipleDatabaseType;

            //if (multipleDatabaseType == MultipleDatabaseType.UnitTest)
            //{
            //    //单元测试
            //    return UnitTestDatabaseConfiguration.UnitTestPillarDbContext;
            //}
            //else 
            if (!this.ContainsKey(multipleDatabaseType))
            {
                throw new NcfDatabaseException($"未发现任何支持此数据库类型的 XNCF 模块：{multipleDatabaseType}", currentDatabaseConfiguration.GetType());
            }

            var xncdDatabaseRegisterCollection = this[multipleDatabaseType];
            if (!xncdDatabaseRegisterCollection.ContainsKey(xncfDatabaseRegisterType))
            {
                throw new NcfDatabaseException($"{xncfDatabaseRegisterType.FullName} 模块未支持数据库：{multipleDatabaseType}", currentDatabaseConfiguration.GetType());
            }

            return xncdDatabaseRegisterCollection[xncfDatabaseRegisterType];
        }


        /// <summary>
        /// 获取指定 DbContext 的数据库实例
        /// </summary>
        /// <param name="connectionString">连接字符串，如果为 null，则默认使用 SenparcDatabaseConfigs.ClientConnectionString</param>
        /// <param name="dbContextOptionsAction">额外配置操作</param>
        /// <param name="xncfDatabaseData">IXncfDatabase 信息（仅在针对 XNCF 进行数据库迁移时有效）</param>
        /// <param name="serviceProvider">ServiceProvider</param>
        /// <returns></returns>
        public T GetDbContext<T>(string connectionString = null, XncfDatabaseData xncfDatabaseData = null,
            Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null, IServiceProvider serviceProvider = null) where T : DbContext
        {
            var dbContextType = typeof(T);
            DbContextOptionsBuilder dbOptionBuilder;

            var dbOptionBuilderType = dbContextType.GetConstructors().First()
                                        .GetParameters().First().ParameterType;

            if (dbOptionBuilderType.GenericTypeArguments.Length > 0)
            {
                //带泛型
                //准备创建 DbContextOptionsBuilder 实例，定义类型
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

            if (UnitTestDatabaseConfiguration.UnitTestPillarDbContext == null)
            {
                //不是单元测试，需要读取数据库

                //获取当前数据库配置
                var currentDatabasConfiguration = DatabaseConfigurationFactory.Instance.Current;
                //指定使用当前数据库
                currentDatabasConfiguration.UseDatabase(
                    dbOptionBuilder,
                    connectionString ?? SenparcDatabaseConnectionConfigs.ClientConnectionString,
                    xncfDatabaseData,
                    dbContextOptionsAction
                    );
            }

            //实例化 DbContext
            T dbContext;
            if (serviceProvider == null)
            {
                dbContext = Activator.CreateInstance(dbContextType, new object[] { dbOptionBuilder.Options }) as T;
            }
            else
            {
                dbContext = Activator.CreateInstance(dbContextType, new object[] { dbOptionBuilder.Options, serviceProvider }) as T;
            }

            if (dbContext == null)
            {
                throw new NcfDatabaseException($"未能创建 {dbContextType.FullName} 的实例", DatabaseConfigurationFactory.Instance.Current.GetType(), null);
            }
            return dbContext;
        }

        /// <summary>
        /// 获取指定 xncfDatabaseRegister 关联的当前数据库实例
        /// </summary>
        /// <param name="xncfDatabaseRegisterType">实现了 IXncfDatabase 的具体类型</param>
        /// <param name="connectionString">连接字符串，如果为 null，则默认使用 SenparcDatabaseConfigs.ClientConnectionString</param>
        /// <param name="dbContextOptionsAction">额外配置操作</param>
        /// <param name="xncfDatabaseData">IXncfDatabase 信息（仅在针对 XNCF 进行数据库迁移时有效）</param>
        /// <param name="serviceProvider">ServiceProvider</param>
        /// <returns></returns>
        public DbContext GetXncfDbContext(Type xncfDatabaseRegisterType, string connectionString = null, XncfDatabaseData xncfDatabaseData = null,
            Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null, IServiceProvider serviceProvider = null)
        {
            if (!typeof(IXncfDatabase).IsAssignableFrom(xncfDatabaseRegisterType))
            {
                throw new NcfDatabaseException($"{nameof(xncfDatabaseRegisterType)} 参数：{xncfDatabaseRegisterType.Name} 必须实现 IXncfDatabase 接口", DatabaseConfigurationFactory.Instance.Current.GetType());
            }

            //获取 DbContext 上下文类型
            var dbContextType = GetXncfDbContextType(xncfDatabaseRegisterType);

            return this.GetType().GetMethod(nameof(GetDbContext))
                .MakeGenericMethod(new Type[] { dbContextType })
                .Invoke(this, new object[] { connectionString, xncfDatabaseData, dbContextOptionsAction, serviceProvider }) as DbContext;

            //return GetDbContext(dbContextType, connectionString, xncfDatabaseData, dbContextOptionsAction, serviceProvider);
        }
    }
}
