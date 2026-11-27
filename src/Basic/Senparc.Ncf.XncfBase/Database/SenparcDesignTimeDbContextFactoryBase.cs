/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenparcDesignTimeDbContextFactoryBase.cs
    File description: SenparcDesignTimeDbContextFactoryBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.24.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.29.1.2 Extend shared chat-tool contracts for admin task initiation.

----------------------------------------------------------------*/

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
    /// Provides design-time DbContext factory behavior for an XNCF database registration.
    /// </summary>
    /// <typeparam name="TSenparcEntities">The concrete runtime DbContext type.</typeparam>
    /// <typeparam name="TXncfDatabaseRegister">The XNCF database registration type.</typeparam>
    public abstract class SenparcDesignTimeDbContextFactoryBase<TSenparcEntities, TXncfDatabaseRegister>
        : SenparcDesignTimeDbContextFactoryBase<TSenparcEntities>
            where TSenparcEntities : DbContext, ISenparcEntitiesDbContext
            where TXncfDatabaseRegister : class, IXncfDatabase, new()
    {
        /// <summary>
        /// Creates the provider-specific runtime DbContext selected by the multiple-database pool.
        /// </summary>
        /// <param name="dbContextOptions">The options configured by the base design-time factory.</param>
        /// <returns>The created XNCF DbContext.</returns>
        public override TSenparcEntities GetDbContextInstance(DbContextOptions<TSenparcEntities> dbContextOptions)
        {
            //Legacy comment translated to English.
            //var databaseRegister = Activator.CreateInstance(typeof(TXncfDatabaseRegister)) as TXncfDatabaseRegister;

            //Legacy comment translated to English.
            var dbContextType = MultipleDatabasePool.Instance.GetXncfDbContextType(typeof(TXncfDatabaseRegister));

            var constructorParams = new List<object>() {
                dbContextOptions
            };

            //Legacy comment translated to English.
            if (dbContextType.GetConstructors().First().GetParameters().Length == 2)
            {
                constructorParams.Add(SenparcDI.GetServiceProvider());//Legacy comment translated to English.
            }

            //Legacy comment translated to English.
            var xncfSenparcEntities = Activator.CreateInstance(dbContextType, constructorParams.ToArray()) as TSenparcEntities;
            return xncfSenparcEntities;
        }

        private readonly TXncfDatabaseRegister _register;


        /// <summary>
        /// Initializes design-time database metadata for the target XNCF registration.
        /// </summary>
        /// <param name="rootDirectoryPath">The project root used to resolve configuration files.</param>
        /// <param name="databaseName">The logical database name used by the design-time factory.</param>
        /// <param name="note">Optional descriptive note stored with database metadata.</param>
        /// <param name="dbMigrationAssemblyName">Optional assembly containing migrations.</param>
        protected SenparcDesignTimeDbContextFactoryBase(string rootDirectoryPath, string databaseName = "Local", string note = null, string dbMigrationAssemblyName = null)
            : base(StartupHelper.GetXncfVersion<TXncfDatabaseRegister>(), rootDirectoryPath, databaseName, note)
        {
            _register = System.Activator.CreateInstance<TXncfDatabaseRegister>();
            //var databaseRegister = _register as IXncfRegister;
            base.XncfDatabaseData = new XncfDatabaseData(_register, dbMigrationAssemblyName);
        }

        /// <inheritdoc />
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

            //Legacy comment translated to English.
            var multipleDatabasePool = MultipleDatabasePool.Instance;
            var dbContextType = multipleDatabasePool.GetXncfDbContextType(typeof(TXncfDatabaseRegister));
            var dbContextName = dbContextType.Name;
            Console.WriteLine($"DbContextName: {dbContextName}");
            Console.WriteLine($"===============================");
        }
    }

    /// <summary>
    /// Provides common EF Core design-time setup used to create NCF DbContexts.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type produced by the factory.</typeparam>
    public abstract class SenparcDesignTimeDbContextFactoryBase<TDbContext>
        : IDesignTimeDbContextFactory<TDbContext>
            where TDbContext : DbContext
    {
        protected virtual Action<IServiceCollection> ServicesAction { get; }
        protected virtual Action<IApplicationBuilder> AppAction { get; }

        public IDatabaseConfiguration DatabaseConfiguration { get; set; }

        /// <summary>
        /// Gets the connection string used by the design-time factory.
        /// </summary>
        public virtual string DatabaseConnectionStr => SenparcDatabaseConnectionConfigs.GetClientConnectionString(); //?? "Server=.\\;Database=NCF;Trusted_Connection=True;integrated security=True;";

        /// <summary>
        /// Creates a concrete DbContext from configured options.
        /// </summary>
        /// <param name="dbContextOptions">The configured DbContext options.</param>
        /// <returns>The created DbContext.</returns>
        public abstract TDbContext GetDbContextInstance(DbContextOptions<TDbContext> dbContextOptions);

        /// <summary>
        /// Gets the callback that applies provider-specific options to a DbContext builder.
        /// </summary>
        public virtual Action</*SqlServerDbContextOptionsBuilder*/IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsAction => (builder, xncfDatabaseData) =>
         {
             DatabaseConfiguration.DbContextOptionsActionBase(builder, xncfDatabaseData);
         };

        private readonly string _ncfVersion;
        private readonly string _note;

        /// <summary>
        /// Gets or sets the module database metadata passed to provider option callbacks.
        /// </summary>
        protected XncfDatabaseData XncfDatabaseData { get; set; }

        /// <summary>
        /// Initializes design-time NCF settings, root paths, and the minimal NCF service registration.
        /// </summary>
        /// <param name="ncfVersion">The NCF version displayed during design-time setup.</param>
        /// <param name="rootDirectoryPath">The application root directory used by configuration and file mapping.</param>
        /// <param name="databaseName">The logical database name used by NCF settings.</param>
        /// <param name="note">An optional version note.</param>
        public SenparcDesignTimeDbContextFactoryBase(string ncfVersion, string rootDirectoryPath, /*XncfDatabaseData xncfDatabaseData = null,*/
            string databaseName = "Local", string note = null)
        {
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
        /// Writes design-time database diagnostics and XNCF registration information to the console.
        /// </summary>
        public abstract void CreateDbContextAction();

        /// <summary>
        /// Builds NCF services, starts the XNCF engine, configures the current database provider,
        /// and creates the design-time DbContext.
        /// </summary>
        /// <param name="args">Design-time command-line arguments retained for factory compatibility.</param>
        /// <returns>The created design-time DbContext.</returns>
        /// <exception cref="NcfDatabaseException">Thrown when no connection string is available.</exception>
        public virtual TDbContext CreateDbContext(string[] args)
        {
            //getdatabaseconfiguration
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

            //Legacy comment translated to English.
            var env = services.BuildServiceProvider().GetService<IHostEnvironment>();

            serviceCollection.AddMemoryCache();//Legacy comment translated to English.
            //Legacy comment translated to English.
            IRegisterService register = RegisterService.Start(senparcSetting);

            //Legacy comment translated to English.
            //Legacy comment translated to English.

            if (DatabaseConnectionStr.IsNullOrEmpty())
            {
                throw new NcfDatabaseException("DatabaseConnectionStr cannot be empty!", DatabaseConfiguration.GetType());
            }

            var sqlConnection = DatabaseConnectionStr;

            Console.WriteLine(Senparc.Ncf.Core.VersionManager.GetVersionNote(_ncfVersion, _note));

            Console.WriteLine("=======  Connection String  =======");
            //Legacy comment translated to English.
            Console.WriteLine("Connection string loaded (sensitive value hidden).");
            Console.WriteLine("===================================");
            Console.WriteLine("");
            Console.WriteLine("=======  Start XNCF Engine  =======");
            var dt1 = DateTime.Now;
            var startEngineRresult = Senparc.Ncf.XncfBase.Register.StartNcfEngine(serviceCollection, config, env, null);//Legacy comment translated to English.
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

            //Legacy comment translated to English.
            var builder = new DbContextOptionsBuilder<TDbContext>();
            DatabaseConfiguration.UseDatabase(builder, sqlConnection, XncfDatabaseData,
            /* Legacy comment translated to English.*/
            DbContextOptionsAction);
            //Legacy comment translated to English.

            return GetDbContextInstance(builder.Options);
        }
    }
}
