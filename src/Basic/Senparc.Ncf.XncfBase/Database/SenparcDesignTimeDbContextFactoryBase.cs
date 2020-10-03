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
using Senparc.Ncf.Database;
using System;

namespace Senparc.Ncf.XncfBase.Database
{
    /// <summary>
    /// 提供给数据库 Migration 使用的 DesignTimeDbContextFactory
    /// </summary>
    /// <typeparam name="TSenparcEntities"></typeparam>
    public abstract class SenparcDesignTimeDbContextFactoryBase<TSenparcEntities, TXncfDatabaseRegister>
        : SenparcDesignTimeDbContextFactoryBase<TSenparcEntities>
            where TSenparcEntities : XncfDatabaseDbContext
            where TXncfDatabaseRegister : class, IXncfDatabase, new()
    {


        public virtual SenparcSetting SenparcSetting => new SenparcSetting();

        public override string SqlConnectionStr => SenparcDatabaseConfigs.ClientConnectionString ?? "Server=.\\;Database=NCF;Trusted_Connection=True;integrated security=True;";

        //public override Action</*SqlServerDbContextOptionsBuilder*/ TDbContextOptionsBuilder> DbContextOptionsAction =>
        //    b =>
        //    {
        //        _register.DbContextOptionsAction(b, null); 
        //        base.DatabaseConfiguration.DbContextOptionsAction(b);
        //    };

        public override TSenparcEntities GetDbContextInstance(DbContextOptions<TSenparcEntities> dbContextOptions)
        {
            //获取 XncfDatabase 对象
            var databaseRegister = Activator.CreateInstance(typeof(TXncfDatabaseRegister)) as TXncfDatabaseRegister;
            //获取 XncfSenparcEntities 实例
            var xncfSenparcEntities = Activator.CreateInstance(databaseRegister.XncfDatabaseDbContextType, new object[] { dbContextOptions }) as TSenparcEntities;
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
            _register = System.Activator.CreateInstance<TXncfDatabaseRegister>();

            var databaseRegister = _register as IXncfRegister;

            base.XncfDatabaseData = new XncfDatabaseData(_register.XncfDatabaseDbContextType,
                                                         dbMigrationAssemblyName,
                                                         databaseRegister?.GetDatabaseMigrationHistoryTableName(),
                                                         _register.DatabaseUniquePrefix);
            Senparc.Ncf.Core.Register.TryRegisterMiniCore();
        }

        public override void CreateDbContextAction()
        {
            Console.WriteLine($"=======  XNCF Database  =======");
            if (_register is IXncfRegister xncfRegister)
            {
                Console.WriteLine($"Name: {xncfRegister.Name}");
                Console.WriteLine($"Menu Name: {xncfRegister.MenuName}");
                Console.WriteLine($"Uid: {xncfRegister.Uid}");
                Console.WriteLine($"Version: {xncfRegister.Version}");
            }
            var dbContextName = _register.XncfDatabaseDbContextType?.Name;
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

        public IDatabaseConfiguration DatabaseConfiguration { get; set; }

        /// <summary>
        /// SQL Server 连接字符串
        /// </summary>
        public virtual string SqlConnectionStr => SenparcDatabaseConfigs.ClientConnectionString ?? "Server=.\\;Database=NCF;Trusted_Connection=True;integrated security=True;";

        /// <summary>
        /// 返回 DbContext 实例
        /// </summary>
        /// <param name="dbContextOptions"></param>
        /// <returns></returns>
        public abstract TDbContext GetDbContextInstance(DbContextOptions<TDbContext> dbContextOptions);

        /// <summary>
        /// 指定程序集等配置，如：
        /// b => systemServiceRegister.DbContextOptionsAction(b, "Senparc.Service")
        /// </summary>
        public virtual Action</*SqlServerDbContextOptionsBuilder*/IRelationalDbContextOptionsBuilderInfrastructure> DbContextOptionsAction =>
            DatabaseConfiguration.DbContextOptionsAction;//默认调用 DatabaseConfiguration 中的 DbContextOptionsAction

        private readonly string _ncfVersion;
        private readonly string _note;

        protected XncfDatabaseData XncfDatabaseData { get; set; }

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

            var sqlConnection = (SqlConnectionStr ??
                SenparcDatabaseConfigs.ClientConnectionString) ??
                "Server=.\\;Database=NCF;Trusted_Connection=True;integrated security=True;";

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
            Console.WriteLine($"======= 耗时: {SystemTime.DiffTotalMS(dt1)} 毫秒 =======");
            Console.WriteLine();

            Console.WriteLine("=======  DatabaseConfiguration  =======");
            Console.WriteLine($"DatabaseConfiguration: {DatabaseConfiguration.GetType().Name}");
            Console.WriteLine($"DatabaseConfiguration.DbContextOptionsBuilderType: {XncfDatabaseData?.XncfDatabaseDbContextType.Name ?? "未指定"}");
            Console.WriteLine($"DbContextOptionsAction: {(DatabaseConfiguration.DbContextOptionsAction == null ? "未指定" : "已指定")}");
            Console.WriteLine($"DatabaseUniquePrefix: {(XncfDatabaseData?.DatabaseUniquePrefix ?? "未指定")}");
            Console.WriteLine("=======================================");
            Console.WriteLine();

            CreateDbContextAction();

            Console.WriteLine("11111111111111111111111111111");


            //创建 DbContextOptionsBuilder 对象
            var builder = new DbContextOptionsBuilder<TDbContext>();
            DatabaseConfiguration.UseDatabase(builder, sqlConnection,
            /* 注意：这里不能用 this.DbContextOptionsAction，否则子类重写将无效！*/
            DbContextOptionsAction);
            //单一使用 SQL Server 的方法：builder.UseSqlServer(sqlConnection, DbContextOptionsAction);//beta6

            return GetDbContextInstance(builder.Options);
        }
    }
}
