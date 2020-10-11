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
        /// <returns></returns>
        public string TryAdd(MultipleMigrationDbContextAttribute multiDbContextAttr, Type xncfDatabaseDbContextType)
        {
            var msg = $"检测到多数据库 DbContext：{multiDbContextAttr.XncfDatabaseRegisterType.FullName}\t>\t{xncfDatabaseDbContextType.FullName} |\t{multiDbContextAttr.MultipleDatabaseType}";

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

            return "\t" + msg;
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
            var currentDatabaseConfiguration = databaseConfigurationFactory.CurrentDatabaseConfiguration;
            //当前数据库类型
            MultipleDatabaseType multipleDatabaseType = currentDatabaseConfiguration.MultipleDatabaseType;
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
        /// 获取指定 xncfDatabaseRegister 关联的当前数据库实例
        /// </summary>
        /// <param name="xncfDatabaseRegisterType">实现了 IXncfDatabase 的具体类型</param>
        /// <param name="connectionString">连接字符串，如果为 null，则默认使用 SenparcDatabaseConfigs.ClientConnectionString</param>
        /// <param name="dbContextOptionsAction">额外配置操作</param>
        /// <param name="xncfDatabaseData">IXncfDatabase 信息（仅在针对 XNCF 进行数据库迁移时有效）</param>
        /// <returns></returns>
        public DbContext GetDbContext(Type xncfDatabaseRegisterType, string connectionString = null, XncfDatabaseData xncfDatabaseData = null, Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null)
        {
            //获取 DbContext 上下文类型
            var dbContextType = GetXncfDbContextType(xncfDatabaseRegisterType);

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

            //获取当前数据库配置
            var currentDatabasConfiguration = DatabaseConfigurationFactory.Instance.CurrentDatabaseConfiguration;
            //指定使用当前数据库
            currentDatabasConfiguration.UseDatabase(
                dbOptionBuilder,
                connectionString ?? SenparcDatabaseConfigs.ClientConnectionString,
                xncfDatabaseData,
                dbContextOptionsAction
                );
            //实例化 DbContext
            var dbContext = Activator.CreateInstance(dbContextType, new object[] { dbOptionBuilder.Options }) as DbContext;
            if (dbContext == null)
            {
                throw new NcfDatabaseException($"未能创建 {dbContextType.FullName} 的实例", DatabaseConfigurationFactory.Instance.CurrentDatabaseConfiguration.GetType(), null);
            }
            return dbContext;
        }
    }
}
