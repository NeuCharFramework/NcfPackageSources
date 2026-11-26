/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BaseNcfUnitTest.cs
    File description: BaseNcfUnitTest related implementation

    Created by: Senparc - 20240708

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.3.29 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.InMemory;
using Senparc.Ncf.UnitTestExtension.Database;
using Senparc.Ncf.UnitTestExtension.Entities;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.SystemCore.Domain.Database;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Senparc.Ncf.UnitTestExtension
{
    /// <summary>
    /// Provides a reusable NCF test host with dependency injection, in-memory
    /// database registration, XNCF modules, and optional seed data.
    /// </summary>
    public class BaseNcfUnitTest
    {
        private IServiceCollection ServiceCollection { get; set; }
        public IConfiguration Configuration { get; set; }

        public IHostEnvironment Env { get; set; }

        protected IRegisterService registerService;
        protected SenparcSetting _senparcSetting;

        protected IServiceProvider _serviceProvider;
        protected static DataList GlobalDataList = new DataList(Guid.NewGuid().ToString());
        protected static ConcurrentDictionary<string, DataList> GlobalDataListCollection = new ConcurrentDictionary<string, DataList>();

        //public BaseNcfUnitTest() : this(null, null)
        //{

        //}

        /// <summary>
        /// Initializes the test host, registers NCF services, configures the
        /// in-memory database, and executes optional seed-data hooks.
        /// </summary>
        /// <param name="servicesRegister">Optional additional service registrations.</param>
        /// <param name="seedDataBuilder">Optional seed-data builder.</param>
        public BaseNcfUnitTest(Action<IServiceCollection> servicesRegister = null, UnitTestSeedDataBuilder seedDataBuilder = null)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var builder = WebApplication.CreateBuilder();
            ServiceCollection = builder.Services;

            var mockEnv = new Mock<IHostEnvironment/*IHostingEnvironment*/>();
            mockEnv.Setup(z => z.ContentRootPath).Returns(() => UnitTestHelper.RootPath);
            mockEnv.Setup(z => z.EnvironmentName).Returns(() => "Test");

            Env = mockEnv.Object;

            servicesRegister?.Invoke(ServiceCollection);

            //Console.WriteLine("Test Data:" + dataLists.ToJson(true, new Newtonsoft.Json.JsonSerializerSettings()
            //{
            //    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            //}));

            //var configuration = builder.Configuration;
            //configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            //      .AddJsonFile($"appsettings.{Env.EnvironmentName}.json", optional: true, reloadOnChange: true);


            BeforeRegisterServiceCollection(ServiceCollection);
            RegisterServiceCollection();
            RegisterServiceCollectionFinished(ServiceCollection);

            BuildServiceProvider();

            RegisterServiceStart();

            var app = builder.Build();
            app.UseNcfDatabase<InMemoryDatabaseConfiguration>();

            app.UseXncfModules(registerService);

            #region Legacy section
            //Legacy comment translated to English.
            var dataList = seedDataBuilder?.ExecuteAsync(this._serviceProvider).GetAwaiter().GetResult();

            dataList ??= new DataList(Guid.Empty.ToString("N"));

            if (!GlobalDataListCollection.ContainsKey(dataList.UUID))
            {
                GlobalDataListCollection[dataList.UUID] = dataList;
                GlobalDataList.AddRange(dataList);

                //Legacy comment translated to English.
                AutoFillSeedData(seedDataBuilder, dataList);
                //Legacy comment translated to English.
                seedDataBuilder?.OnExecutedAsync(this._serviceProvider, dataList).GetAwaiter().GetResult();
            }

            #endregion
        }

        /// <summary>
        /// Reflects registered seed collections into the in-memory test DbContext and saves them.
        /// </summary>
        /// <param name="seedDataBuilder">The optional seed builder that supplied the data.</param>
        /// <param name="dataLists">The entity-type keyed seed collections.</param>
        private void AutoFillSeedData(UnitTestSeedDataBuilder seedDataBuilder, DataList dataLists)
        {
            using (var scope = this._serviceProvider.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;
                var dbContext = serviceProvider.GetService<NcfUnitTestEntities>();
                //Legacy comment translated to English.
                foreach (var dataListKv in dataLists)
                {
                    var type = dataListKv.Key;
                    var dataList = dataListKv.Value;

                    //settings DbSet<T>
                    var binder = Type.DefaultBinder;
                    var method = dbContext.GetType().GetMethod(nameof(dbContext.Set), BindingFlags.Instance | BindingFlags.Public, binder, Type.EmptyTypes, null).MakeGenericMethod(type);
                    var dbSet = method.Invoke(dbContext, null);

                    //Legacy comment translated to English.
                    var listType = typeof(List<>).MakeGenericType(type);
                    var castMethod = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(type);
                    var toListMethod = typeof(Enumerable).GetMethod("ToList").MakeGenericMethod(type);

                    var castData = castMethod.Invoke(null, new object[] { dataList });
                    var listData = toListMethod.Invoke(null, new object[] { castData });

                    //Legacy comment translated to English.
                    var addRangeMethod = dbSet.GetType().GetMethod("AddRange", new[] { listType });
                    addRangeMethod.Invoke(dbSet, new[] { listData });

                    dbContext.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Extension hook invoked before the base test service collection is built.
        /// </summary>
        /// <param name="services">The mutable service collection.</param>
        protected virtual void BeforeRegisterServiceCollection(IServiceCollection services)
        {
            Console.WriteLine("BaseNcfUnitTest.BeforeRegisterServiceCollection");
        }

        /// <summary>
        /// Extension hook invoked after the base test service collection registration finishes.
        /// </summary>
        /// <param name="services">The registered service collection.</param>
        protected virtual void RegisterServiceCollectionFinished(IServiceCollection services)
        {

        }

        /// <summary>
        /// Ensures seed data for <typeparamref name="T"/> exists in the global
        /// test data collection.
        /// </summary>
        /// <typeparam name="T">The entity type whose seed data is checked.</typeparam>
        public void TryInitSeedData<T>() where T : EntityBase
        {
            if (!GlobalDataList.ContainsKey(typeof(T)))
            {
                GlobalDataList[typeof(T)] = new List<object>();
            }
        }



        /// <summary>
        /// Builds the test service provider from the supplied or current service collection.
        /// </summary>
        /// <param name="services">Optional replacement service collection.</param>
        public void BuildServiceProvider(IServiceCollection services = null)
        {
            if (services != null)
            {
                this.ServiceCollection = services;
            }
            this._serviceProvider = this.ServiceCollection.BuildServiceProvider();

        }

        /// <summary>
        /// Extension hook for test setup actions that require the configured service collection.
        /// </summary>
        protected virtual void ActionInServiceCollection()
        {

        }

        /// <summary>
        /// Registers global Senparc services, in-memory database services, and
        /// the test host's scoped data context.
        /// </summary>
        public void RegisterServiceCollection()
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(UnitTestHelper.GetAppSettingsFile(Env), false, false);
            var config = configBuilder.Build();
            Configuration = config;

            _senparcSetting = new SenparcSetting() { IsDebug = true };
            config.GetSection("SenparcSetting").Bind(_senparcSetting);

            ServiceCollection.AddSenparcGlobalServices(config);
            ServiceCollection.AddMemoryCache();//Legacy comment translated to English.

            ActionInServiceCollection();

            //Legacy comment translated to English.
            MultipleDatabasePool.UnitTestPillarDbContext = typeof(NcfUnitTestEntities);

            var result = Senparc.Ncf.XncfBase.Register.StartNcfEngine(ServiceCollection, Configuration, Env, null);

            //Legacy comment translated to English.
            ServiceCollection.AddScoped<INcfDbData, NcfUnitTestDataDb>(s =>
            {
                var dbContext = s.GetService<NcfUnitTestEntities>();
                return new NcfUnitTestDataDb(dbContext);
            });

            ServiceCollection.AddScoped<INcfClientDbData, NcfUnitTestDataDb>(s =>
            {
                var dbContext = s.GetService<NcfUnitTestEntities>();
                return new NcfUnitTestDataDb(dbContext);
            });

            ServiceCollection.AddScoped<NcfUnitTestEntities>(s =>
            {
                //Legacy comment translated to English.
                var options = new DbContextOptionsBuilder<NcfUnitTestEntities>()
                                    .UseInMemoryDatabase(databaseName: "UnitTestDb")
                                    //Legacy comment translated to English.
                                    .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                                    .Options;
                var dbContext = new NcfUnitTestEntities(options, s, GlobalDataList);

                return dbContext;

            });
        }

        /// <summary>
        /// Starts the Senparc global registration service and optionally enables
        /// Redis cache strategy discovery.
        /// </summary>
        /// <param name="autoScanExtensionCacheStrategies">Whether extension cache strategies are scanned.</param>
        public void RegisterServiceStart(bool autoScanExtensionCacheStrategies = false)
        {
            //Legacy comment translated to English.
            registerService = Senparc.CO2NET.AspNet.RegisterServices.RegisterService.Start(Env, _senparcSetting)
                .UseSenparcGlobal(autoScanExtensionCacheStrategies);

            //Legacy comment translated to English.
            var redisConfigurationStr = _senparcSetting.Cache_Redis_Configuration;
            var useRedis = !string.IsNullOrEmpty(redisConfigurationStr) && redisConfigurationStr != "#{Cache_Redis_Configuration}#"/*default value; disabled*/;
            if (useRedis)//Legacy comment translated to English.
            {
                /* description：

                 * Legacy comment translated to English.

                Legacy comment translated to English.

                 */
                Senparc.CO2NET.Cache.CsRedis.Register.SetConfigurationOption(redisConfigurationStr);
                Console.WriteLine("完成 Redis 设置");

                //Legacy comment translated to English.
                Senparc.CO2NET.Cache.CsRedis.Register.UseKeyValueRedisNow();//Legacy comment translated to English.
                Console.WriteLine("启用 Redis UseKeyValue 策略");

                //Legacy comment translated to English.

                //Legacy comment translated to English.
                //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisObjectCacheStrategy.Instance);//key-value pair
                //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisHashSetObjectCacheStrategy.Instance);//HashSet
            }

        }
    }
}
