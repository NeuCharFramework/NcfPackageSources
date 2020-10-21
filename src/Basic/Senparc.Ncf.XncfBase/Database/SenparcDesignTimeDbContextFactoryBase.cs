using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using System;
using System.Linq;
using System.Text;

namespace Senparc.Ncf.XncfBase.Database
{
    /// <summary>
    /// 提供给数据库 Migration 使用的 DesignTimeDbContextFactory
    /// </summary>
    /// <typeparam name="TSenparcEntities"></typeparam>
    public abstract class SenparcDesignTimeDbContextFactoryBase<TSenparcEntities, TXncfDatabaseRegister>
        : SenparcDesignTimeDbContextFactoryBase<TSenparcEntities>
            where TSenparcEntities : DbContext, ISenparcEntities
            where TXncfDatabaseRegister : class, IXncfDatabase, new()
    {
        public override TSenparcEntities GetDbContextInstance(DbContextOptions<TSenparcEntities> dbContextOptions)
        {
            //获取 XncfDatabase 对象
            //var databaseRegister = Activator.CreateInstance(typeof(TXncfDatabaseRegister)) as TXncfDatabaseRegister;

            //获取当前适用的 DbContext 类型
            var dbContextType = MultipleDatabasePool.Instance.GetXncfDbContextType(typeof(TXncfDatabaseRegister));

            //获取 XncfSenparcEntities 实例
            var xncfSenparcEntities = Activator.CreateInstance(dbContextType, new object[] { dbContextOptions }) as TSenparcEntities;
            return xncfSenparcEntities;
        }

        private readonly TXncfDatabaseRegister _register;

        private static string GetXncfVersion<TXncfDatabaseRegister>()
            where TXncfDatabaseRegister : class, IXncfDatabase, new()
        {
            try
            {
                var register = System.Activator.CreateInstance<TXncfDatabaseRegister>() as TXncfDatabaseRegister;
                if (register is IXncfRegister xncfRegister)
                {
                    return $" / XNCF {xncfRegister.Name} {xncfRegister.Version}";
                }
                return "Dev";
            }
            catch
            {
                return "Dev / Not IXncfRegister";
            }
        }


        protected SenparcDesignTimeDbContextFactoryBase(string rootDictionaryPath, string databaseName = "Local", string note = null, string dbMigrationAssemblyName = null)
            : base(GetXncfVersion<TXncfDatabaseRegister>(), rootDictionaryPath, databaseName, note)
        {
            //注释可能出现中文，对中文环境可以配置使用 GB2312
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.GetEncoding("GB2312");

            _register = System.Activator.CreateInstance<TXncfDatabaseRegister>();
            //var databaseRegister = _register as IXncfRegister;
            base.XncfDatabaseData = new XncfDatabaseData(_register, dbMigrationAssemblyName);
        }

        public override void CreateDbContextAction()
        {
            var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.CurrentDatabaseConfiguration;
            Console.WriteLine($"=======  XNCF Database  =======");
            Console.WriteLine($"Current System Database Type: {currentDatabaseConfiguration.MultipleDatabaseType}");
            Console.WriteLine();

            if (_register is IXncfRegister xncfRegister)
            {
                Console.WriteLine($"Name: {xncfRegister.Name}");
                Console.WriteLine($"Menu Name: {xncfRegister.MenuName}");
                Console.WriteLine($"Uid: {xncfRegister.Uid}");
                Console.WriteLine($"Version: {xncfRegister.Version}");
            }

            //获取当前适用的 DbContext 类型
            var multipleDatabasePool = MultipleDatabasePool.Instance;
            var dbContextType = multipleDatabasePool.GetXncfDbContextType(typeof(TXncfDatabaseRegister));
            var dbContextName = dbContextType.Name;
            Console.WriteLine($"DbContextName: {dbContextName}");
            Console.WriteLine($"===============================");
        }
    }

    /// <summary>
    /// 提供给数据库 Migration 使用的 DesignTimeDbContextFactory
    /// <para>（针对非 XNCF 模块的普通 DbContext，在Senparc.Web 项目下进行 Add-Migration 等操作）</para>
    /// </summary>
    /// <typeparam name="TDbContext"></typeparam>
    public abstract class SenparcDesignTimeDbContextFactoryBase<TDbContext>
        : IDesignTimeDbContextFactory<TDbContext>
            where TDbContext : DbContext
    {
        protected virtual Action<IServiceCollection> ServicesAction { get; }

        public IDatabaseConfiguration DatabaseConfiguration { get; set; }

        /// <summary>
        /// SQL Server 连接字符串
        /// </summary>
        public virtual string DatabaseConnectionStr => SenparcDatabaseConnectionConfigs.ClientConnectionString; //?? "Server=.\\;Database=NCF;Trusted_Connection=True;integrated security=True;";

        /// <summary>
        /// 返回 DbContext 实例
        /// </summary>
        /// <param name="dbContextOptions"></param>
        /// <returns></returns>
        public abstract TDbContext GetDbContextInstance(DbContextOptions<TDbContext> dbContextOptions);

        /// <summary>
        /// 指定程序集等配置，如：
        /// b => systemServiceRegister.DbContextOptionsAction(b, "Senparc.Service")
        /// <para>注意：如果重写，最后一定要执行 base.DbContextOptionsAction() </para>
        /// </summary>
        public virtual Action</*SqlServerDbContextOptionsBuilder*/IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsAction => (builder, xncfDatabaseData) =>
         {
             DatabaseConfiguration.DbContextOptionsActionBase(builder, xncfDatabaseData);
         };

        private readonly string _ncfVersion;
        private readonly string _note;

        /// <summary>
        /// XncfDatabaseData
        /// </summary>
        protected XncfDatabaseData XncfDatabaseData { get; set; }

        ///// <summary>
        ///// 特殊类型，例如 SystemServiceEntities
        ///// </summary>
        //protected Type SpecificSenparcEntites { get; } = null;


        /// <summary>
        /// SenparcDesignTimeDbContextFactoryBase 构造函数
        /// </summary>
        /// <param name="ncfVersion">NCF 版本号</param>
        /// <param name="rootDictionaryPath">将要设置的CO2NET.Config.RootDictionaryPath，一般为 Senparc.Web 或具有 App_Data/Database/SenparcConfig.config 配置文件的目录</param>
        /// <param name="databaseName">数据库名称，默认为 Local，即 Senparc.Web/appsettings.json 中的 DatabaseName</param>
        /// <param name="note">在日志中输出额外信息</param>
        public SenparcDesignTimeDbContextFactoryBase(string ncfVersion, string rootDictionaryPath, /*XncfDatabaseData xncfDatabaseData = null,*/
            string databaseName = "Local", string note = null)
        {
            SiteConfig.SenparcCoreSetting.DatabaseName = databaseName;
            CO2NET.Config.RootDictionaryPath = rootDictionaryPath;
            //XncfDatabaseData = xncfDatabaseData;
            this._ncfVersion = ncfVersion;
            this._note = note;

            Senparc.Ncf.Core.Register.TryRegisterMiniCore(ServicesAction);
        }


        /// <summary>
        /// 创建过程的其他代码
        /// </summary>
        public abstract void CreateDbContextAction();
        public virtual TDbContext CreateDbContext(string[] args)
        {

            //获取数据库配置
            DatabaseConfiguration = DatabaseConfigurationFactory.Instance.CurrentDatabaseConfiguration;

            try
            {
                var repository = LogManager.CreateRepository("NETCoreRepository");
            }
            catch
            {
            }
            var serviceCollection = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();
            var config = configBuilder.Build();
            var senparcSetting = new SenparcSetting() { IsDebug = true };
            serviceCollection.AddSenparcGlobalServices(config);
            serviceCollection.AddMemoryCache();//使用内存缓存
            //修复 https://github.com/NeuCharFramework/NCF/issues/13 发现的问题（在非Web环境下无法得到网站根目录路径）
            IRegisterService register = RegisterService.Start(senparcSetting);

            //如果运行 Add-Migration 命令，并且获取不到正确的网站根目录，此处可能无法自动获取到连接字符串（上述#13问题），
            //也可通过下面已经注释的的提供默认值方式解决（不推荐）

            if (DatabaseConnectionStr.IsNullOrEmpty())
            {
                throw new NcfDatabaseException("DatabaseConnectionStr 不能为空！", DatabaseConfiguration.GetType());
            }

            var sqlConnection = DatabaseConnectionStr;

            Console.WriteLine(Senparc.Ncf.Core.VersionManager.GetVersionNote(_ncfVersion, _note));

            Console.WriteLine("=======  Connection String  =======");
            Console.WriteLine(sqlConnection);
            Console.WriteLine("===================================");
            Console.WriteLine("");
            Console.WriteLine("=======  Start XNCF Engine  =======");
            var dt1 = DateTime.Now;
            var startEngineRresult = Senparc.Ncf.XncfBase.Register.StartEngine(serviceCollection, config);
            if (!startEngineRresult.IsNullOrEmpty())
            {
                Console.Write(startEngineRresult);
            }
            Console.WriteLine("======= XNCF Engine Started =======");
            Console.WriteLine($"======= Cost: {SystemTime.DiffTotalMS(dt1)} ms =======");
            Console.WriteLine();

            Console.WriteLine("=======  DatabaseConfiguration  =======");
            Console.WriteLine($"DatabaseConfiguration: {DatabaseConfiguration.GetType().Name}");

            if (XncfDatabaseData != null)
            {
                if (MultipleDatabasePool.Instance.TryGetValue(DatabaseConfiguration.MultipleDatabaseType, out var xncfDbContextTypes))
                {
                    Console.WriteLine($"Supported XncfDbContextTypes: {string.Join(",", xncfDbContextTypes)}");
                }
                else
                {
                    Console.WriteLine($"Supported XncfDbContextTypes: NONE !!!");
                }
            }

            Console.WriteLine($"DbContextOptionsAction Extension Action: {(DatabaseConfiguration.DbContextOptionsActionExtension == null ? "n.s." : "Specified")}");
            Console.WriteLine($"DatabaseUniquePrefix: {(XncfDatabaseData?.XncfDatabaseRegister?.DatabaseUniquePrefix ?? "n.s.")}");
            Console.WriteLine("=======================================");
            Console.WriteLine();

            CreateDbContextAction();

            //创建 DbContextOptionsBuilder 对象
            var builder = new DbContextOptionsBuilder<TDbContext>();
            DatabaseConfiguration.UseDatabase(builder, sqlConnection, XncfDatabaseData,
            /* 注意：这里不能用 this.DbContextOptionsAction，否则子类重写将无效！*/
            DbContextOptionsAction);
            //单一使用 SQL Server 的方法：builder.UseSqlServer(sqlConnection, DbContextOptionsAction);//beta6

            return GetDbContextInstance(builder.Options);
        }
    }
}
