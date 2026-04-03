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
        /// <param name="migrationDbContextType">The XncfDatabaseDbContext type used at design time (or runtime for Database.Migrate() operations)</param>
        /// <param name="runtimeDbContextType">The XncfDatabaseDbContext type used at runtime (usually when querying)</param>
        public XncfDatabaseDbContextWapper(Type migrationDbContextType, Type runtimeDbContextType)
        {
            MigrationDbContextType = migrationDbContextType;
            RuntimeDbContextType = runtimeDbContextType;
        }

        public Type MigrationDbContextType { get; set; }
        public Type RuntimeDbContextType { get; set; }
    }

    /// <summary>
    ///Multiple database configuration pool
    /// <para>Value is Dictionary<Type/* IXncfDatabase Register type */, Type/* database XncfDatabaseDbContext type */></para>
    /// </summary>
    public class MultipleDatabasePool
        : Dictionary<MultipleDatabaseType, Dictionary<Type/* IXncfDatabaseRegisterType*/, Type/* Database XncfDatabaseDbContext type */>>
    {
        #region 单例

        MultipleDatabasePool() { }

        /// <summary>
        ///Global singleton of DatabaseConfigurationFactory
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
        /// DbContext for unit testing
        /// </summary>
        public static Type UnitTestPillarDbContext { get; set; } = null;

        /// <summary>
        ///Add configuration
        /// </summary>
        /// <param name="multiDbContextAttr"></param>
        /// <param name="xncfDatabaseDbContextType">Type that implements the IXncfDatabase interface</param>
        /// <param name="logColumnWidth">Width of the output log table</param>
        /// <returns></returns>
        public string TryAdd(MultipleMigrationDbContextAttribute multiDbContextAttr, Type xncfDatabaseDbContextType, int[] logColumnWidth)
        {
            var msg = $"| {multiDbContextAttr.XncfDatabaseRegisterType.FullName.PadRight(logColumnWidth[0])}| {xncfDatabaseDbContextType.Name.PadRight(logColumnWidth[1])}| {multiDbContextAttr.MultipleDatabaseType.ToString().PadRight(logColumnWidth[2])}";

            //Check if MultipleDatabaseType is already included 
            if (!this.ContainsKey(multiDbContextAttr.MultipleDatabaseType))
            {
                //Add MultipleDatabaseType corresponding collection
                this[multiDbContextAttr.MultipleDatabaseType] = new Dictionary<Type, Type>();
            }

            //Add configuration
            this[multiDbContextAttr.MultipleDatabaseType][multiDbContextAttr.XncfDatabaseRegisterType] = xncfDatabaseDbContextType;

            //Synchronously added to XncfDatabaseDbContextPool
            XncfDatabaseDbContextPool.Instance.TryAdd(multiDbContextAttr, xncfDatabaseDbContextType);

            return msg;
        }

        /// <summary>
        /// Get the current database context (DbContext) associated with the specified IXncfDatabase
        /// </summary>
        /// <param name="xncfDatabaseRegister">Entity that implements IXncfDatabase</param>
        /// <returns></returns>
        public Type GetXncfDbContextType(IXncfDatabase xncfDatabaseRegister)
        {
            return GetXncfDbContextType(xncfDatabaseRegister.GetType());
        }

        /// <summary>
        /// Get the current database context (DbContext) associated with the specified IXncfDatabase
        /// </summary>
        /// <param name="xncfDatabaseRegisterType">Implements the specific type of IXncfDatabase</param>
        /// <returns></returns>
        public Type GetXncfDbContextType(Type xncfDatabaseRegisterType)
        {
            //Database configuration factory
            var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
            //Current database configuration
            var currentDatabaseConfiguration = databaseConfigurationFactory.Current;
            //Current database type
            MultipleDatabaseType multipleDatabaseType = currentDatabaseConfiguration.MultipleDatabaseType;

            if (multipleDatabaseType == MultipleDatabaseType.InMemory)
            {
                //Unit testing
                return UnitTestPillarDbContext ?? throw new NcfExceptionBase($"当前数据库类型为 {multipleDatabaseType}，需要指定 {nameof(UnitTestPillarDbContext)}！");
            }
            else if (!this.ContainsKey(multipleDatabaseType))
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
        /// Get the database instance of the specified DbContext
        /// </summary>
        /// <param name="connectionString">Connection string, if null, defaults to SenparcDatabaseConfigs.ClientConnectionString</param>
        /// <param name="dbContextOptionsAction">Additional configuration operations</param>
        /// <param name="xncfDatabaseData">IXncfDatabase information (valid only when doing database migration for XNCF)</param>
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
                //With generics
                //Prepare to create a DbContextOptionsBuilder instance and define the type
                dbOptionBuilderType = typeof(DbContextOptionsBuilder<>);
                //dbOptionBuilderType = typeof(RelationalDbContextOptionsBuilder<,>);
                //Get the generic object type, such as: DbContextOptionsBuilder<SenparcEntities>
                dbOptionBuilderType = dbOptionBuilderType.MakeGenericType(dbContextType);

                //Create a DbContextOptionsBuilder instance
                dbOptionBuilder = Activator.CreateInstance(dbOptionBuilderType) as DbContextOptionsBuilder;
            }
            else
            {
                //Without generics
                dbOptionBuilder = new DbContextOptionsBuilder();
            }

            //if (UnitTestDatabaseConfiguration.UnitTestPillarDbContext == null)
            {
                //Not a unit test, needs to read the database

                //Get the current database configuration
                var currentDatabasConfiguration = DatabaseConfigurationFactory.Instance.Current;
                //Specify to use the current database
                currentDatabasConfiguration.UseDatabase(
                    dbOptionBuilder,
                    connectionString ?? (SenparcDatabaseConnectionConfigs.GetClientConnectionString()),
                    xncfDatabaseData,
                    dbContextOptionsAction
                    );
            }

            //Instantiate DbContext
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
        /// Get the current database instance associated with the specified xncfDatabaseRegister
        /// </summary>
        /// <param name="xncfDatabaseRegisterType">Implements the specific type of IXncfDatabase</param>
        /// <param name="connectionString">Connection string, if null, defaults to SenparcDatabaseConfigs.ClientConnectionString</param>
        /// <param name="dbContextOptionsAction">Additional configuration operations</param>
        /// <param name="xncfDatabaseData">IXncfDatabase information (valid only when doing database migration for XNCF)</param>
        /// <param name="serviceProvider">ServiceProvider</param>
        /// <returns></returns>
        public DbContext GetXncfDbContext(Type xncfDatabaseRegisterType, string connectionString = null, XncfDatabaseData xncfDatabaseData = null,
            Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null, IServiceProvider serviceProvider = null)
        {
            if (!typeof(IXncfDatabase).IsAssignableFrom(xncfDatabaseRegisterType))
            {
                throw new NcfDatabaseException($"{nameof(xncfDatabaseRegisterType)} 参数：{xncfDatabaseRegisterType.Name} 必须实现 IXncfDatabase 接口", DatabaseConfigurationFactory.Instance.Current.GetType());
            }

            //Get the DbContext context type
            var dbContextType = GetXncfDbContextType(xncfDatabaseRegisterType);

            return this.GetType().GetMethod(nameof(GetDbContext))
                .MakeGenericMethod(new Type[] { dbContextType })
                .Invoke(this, new object[] { connectionString, xncfDatabaseData, dbContextOptionsAction, serviceProvider }) as DbContext;

            //return GetDbContext(dbContextType, connectionString, xncfDatabaseData, dbContextOptionsAction, serviceProvider);
        }
    }
}
