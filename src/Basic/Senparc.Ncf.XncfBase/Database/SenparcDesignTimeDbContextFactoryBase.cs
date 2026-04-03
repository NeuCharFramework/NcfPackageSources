using log4net;
using Microsoft.AspNetCore.Builder;
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
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Ncf.XncfBase.Database
{
    /// <summary>
    /// DesignTimeDbContextFactory provided to database Migration
    /// </summary>
    /// <typeparam name="TSenparcEntities"></typeparam>
    public abstract class SenparcDesignTimeDbContextFactoryBase<TSenparcEntities, TXncfDatabaseRegister>
        : SenparcDesignTimeDbContextFactoryBase<TSenparcEntities>
            where TSenparcEntities : DbContext, ISenparcEntitiesDbContext
            where TXncfDatabaseRegister : class, IXncfDatabase, new()
    {
        public override TSenparcEntities GetDbContextInstance(DbContextOptions<TSenparcEntities> dbContextOptions)
        {
            //Get the XncfDatabase object
            //var databaseRegister = Activator.CreateInstance(typeof(TXncfDatabaseRegister)) as TXncfDatabaseRegister;

            //Get the currently applicable DbContext type
            var dbContextType = MultipleDatabasePool.Instance.GetXncfDbContextType(typeof(TXncfDatabaseRegister));

            var constructorParams = new List<object>() {
                dbContextOptions
            };

            //Determine the number of constructor parameters
            if (dbContextType.GetConstructors().First().GetParameters().Length == 2)
            {
                constructorParams.Add(SenparcDI.GetServiceProvider());//Add a second parameter (currently only used by system base objects such as SystemServiceEntities)
            }

            //Get XncfSenparcEntities instance
            var xncfSenparcEntities = Activator.CreateInstance(dbContextType, constructorParams.ToArray()) as TSenparcEntities;
            return xncfSenparcEntities;
        }

        private readonly TXncfDatabaseRegister _register;


        protected SenparcDesignTimeDbContextFactoryBase(string rootDirectoryPath, string databaseName = "Local", string note = null, string dbMigrationAssemblyName = null)
            : base(StartupHelper.GetXncfVersion<TXncfDatabaseRegister>(), rootDirectoryPath, databaseName, note)
        {
            _register = System.Activator.CreateInstance<TXncfDatabaseRegister>();
            //var databaseRegister = _register as IXncfRegister;
            base.XncfDatabaseData = new XncfDatabaseData(_register, dbMigrationAssemblyName);
        }

        public override void CreateDbContextAction()
        {
            var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.Current;
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

            //Get the currently applicable DbContext type
            var multipleDatabasePool = MultipleDatabasePool.Instance;
            var dbContextType = multipleDatabasePool.GetXncfDbContextType(typeof(TXncfDatabaseRegister));
            var dbContextName = dbContextType.Name;
            Console.WriteLine($"DbContextName: {dbContextName}");
            Console.WriteLine($"===============================");
        }
    }

    /// <summary>
    /// DesignTimeDbContextFactory provided to database Migration
    /// <para>(For ordinary DbContext of non-XNCF modules, perform Add-Migration and other operations under the Senparc.Web project)</para>
    /// </summary>
    /// <typeparam name="TDbContext"></typeparam>
    public abstract class SenparcDesignTimeDbContextFactoryBase<TDbContext>
        : IDesignTimeDbContextFactory<TDbContext>
            where TDbContext : DbContext
    {
        protected virtual Action<IServiceCollection> ServicesAction { get; }
        protected virtual Action<IApplicationBuilder> AppAction { get; }

        public IDatabaseConfiguration DatabaseConfiguration { get; set; }

        /// <summary>
        ///database connection string
        /// </summary>
        public virtual string DatabaseConnectionStr => SenparcDatabaseConnectionConfigs.GetClientConnectionString(); //?? "Server=.\\;Database=NCF;Trusted_Connection=True;integrated security=True;";

        /// <summary>
        /// Returns the DbContext instance
        /// </summary>
        /// <param name="dbContextOptions"></param>
        /// <returns></returns>
        public abstract TDbContext GetDbContextInstance(DbContextOptions<TDbContext> dbContextOptions);

        /// <summary>
        /// Specify assembly and other configurations, such as:
        /// b => systemServiceRegister.DbContextOptionsAction(b, "Senparc.Service")
        /// <para>Note: If you rewrite, you must execute base.DbContextOptionsAction() at the end </para>
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
        ///// Special types, such as SystemServiceEntities
        ///// </summary>
        //protected Type SpecificSenparcEntites { get; } = null;


        /// <summary>
        /// SenparcDesignTimeDbContextFactoryBase constructor
        /// </summary>
        /// <param name="ncfVersion">NCF version number</param>
        /// <param name="rootDirectoryPath">CO2NET.Config.RootDirectoryPath to be set, usually Senparc.Web or the directory with the App_Data/Database/SenparcConfig.config configuration file</param>
        /// <param name="databaseName">Database name, default is Local, that is, DatabaseName in Senparc.Web/appsettings.json</param>
        /// <param name="note">Output additional information in the log</param>
        public SenparcDesignTimeDbContextFactoryBase(string ncfVersion, string rootDirectoryPath, /*XncfDatabaseData xncfDatabaseData = null,*/
            string databaseName = "Local", string note = null)
        {
            //Comments may appear in Chinese, and the Chinese environment can be configured to use GB2312
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            SiteConfig.SenparcCoreSetting.DatabaseName = databaseName;
            CO2NET.Config.RootDirectoryPath = rootDirectoryPath;
            //XncfDatabaseData = xncfDatabaseData;
            this._ncfVersion = ncfVersion;
            this._note = note;

            Senparc.Ncf.Core.Register.TryRegisterMiniCore(ServicesAction, AppAction);
        }


        /// <summary>
        /// Other code for the creation process
        /// </summary>
        public abstract void CreateDbContextAction();
        public virtual TDbContext CreateDbContext(string[] args)
        {
            //Get database configuration
            DatabaseConfiguration = DatabaseConfigurationFactory.Instance.Current;

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
            var services = serviceCollection.AddSenparcGlobalServices(config);

            //TODO:env parameter is used starting from v0.11 and needs further testing - Jeffrey 2021.12.15
            var env = services.BuildServiceProvider().GetService<IHostEnvironment>();

            serviceCollection.AddMemoryCache();//Use memory cache
            //Fix the problem found in https://github.com/NeuCharFramework/NCF/issues/13 (the website root directory path cannot be obtained in non-Web environment)
            IRegisterService register = RegisterService.Start(senparcSetting);

            //If you run the Add-Migration command and cannot obtain the correct website root directory, the connection string may not be automatically obtained here (issue #13 above),
            //It can also be solved by providing default values ​​as commented below (not recommended)

            if (DatabaseConnectionStr.IsNullOrEmpty())
            {
                throw new NcfDatabaseException("DatabaseConnectionStr cannot be empty!", DatabaseConfiguration.GetType());
            }

            var sqlConnection = DatabaseConnectionStr;

            Console.WriteLine(Senparc.Ncf.Core.VersionManager.GetVersionNote(_ncfVersion, _note));

            Console.WriteLine("=======  Connection String  =======");
            Console.WriteLine(sqlConnection);
            Console.WriteLine("===================================");
            Console.WriteLine("");
            Console.WriteLine("=======  Start XNCF Engine  =======");
            var dt1 = DateTime.Now;
            var startEngineRresult = Senparc.Ncf.XncfBase.Register.StartNcfEngine(serviceCollection, config, env, null);//TODO:env parameter is used starting from v0.11 and needs further testing - Jeffrey 2021.12.15
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

            //Create a DbContextOptionsBuilder object
            var builder = new DbContextOptionsBuilder<TDbContext>();
            DatabaseConfiguration.UseDatabase(builder, sqlConnection, XncfDatabaseData,
            /* Note: this.DbContextOptionsAction cannot be used here, otherwise subclass rewriting will be invalid!*/
            DbContextOptionsAction);
            //Single method of using SQL Server: builder.UseSqlServer(sqlConnection, DbContextOptionsAction);//beta6

            return GetDbContextInstance(builder.Options);
        }
    }
}
