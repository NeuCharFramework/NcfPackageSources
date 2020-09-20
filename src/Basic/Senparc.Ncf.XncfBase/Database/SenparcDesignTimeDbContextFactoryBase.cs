using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

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

        public override Action<SqlServerDbContextOptionsBuilder> SqlServerOptionsAction =>
            b =>
            {
                _register.DbContextOptionsAction(b, null);
                b.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: new int[] { 2 });
            };

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

        protected SenparcDesignTimeDbContextFactoryBase(string rootDictionaryPath, string databaseName = "Local", string note = null)
            : base(GetXncfVersion<TXncfDatabaseRegister>(), rootDictionaryPath, databaseName, note)
        {
            _register = System.Activator.CreateInstance<TXncfDatabaseRegister>() as TXncfDatabaseRegister;
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
            var dbContextName = _register.XncfDatabaseDbContextType.GetType().Name;
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
        public abstract Action<SqlServerDbContextOptionsBuilder> SqlServerOptionsAction { get; }

        protected readonly string _rootDictionaryPath;
        private readonly string _ncfVersion;
        private readonly string _note;


        /// <summary>
        /// SenparcDesignTimeDbContextFactoryBase 构造函数
        /// </summary>
        /// <param name="ncfVersion">NCF 版本号</param>
        /// <param name="rootDictionaryPath">将要设置的CO2NET.Config.RootDictionaryPath，一般为 Senparc.Web 或具有 App_Data/Database/SenparcConfig.config 配置文件的目录</param>
        /// <param name="databaseName">数据库名称，默认为 Local，即 Senparc.Web/appsettings.json 中的 DatabaseName</param>
        /// <param name="note">在日志中输出额外信息</param>
        public SenparcDesignTimeDbContextFactoryBase(string ncfVersion, string rootDictionaryPath,
            string databaseName = "Local", string note = null)
        {
            SiteConfig.SenparcCoreSetting.DatabaseName = databaseName;
            CO2NET.Config.RootDictionaryPath = rootDictionaryPath;
            _rootDictionaryPath = rootDictionaryPath;
            this._ncfVersion = ncfVersion;
            this._note = note;
        }

        /// <summary>
        /// 创建过程的其他代码
        /// </summary>
        public abstract void CreateDbContextAction();

        public virtual TDbContext CreateDbContext(string[] args)
        {
            var repository = LogManager.CreateRepository("NETCoreRepository");
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
            if (!startEngineRresult.IsNullOrWhiteSpace())
            {
                Console.WriteLine(startEngineRresult);
            }
            Console.WriteLine("======= XNCF Engine Started =======");
            Console.WriteLine($"======= 耗时: {SystemTime.DiffTotalMS(dt1)} 毫秒 =======");

            CreateDbContextAction();

            var builder = new DbContextOptionsBuilder<TDbContext>();
            builder.UseSqlServer(sqlConnection, SqlServerOptionsAction);//beta6

            return GetDbContextInstance(builder.Options);
        }
    }
}
