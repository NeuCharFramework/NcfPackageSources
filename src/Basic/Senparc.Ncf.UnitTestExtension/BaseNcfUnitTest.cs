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
        /// Constructor to initialize the service provider and seed data
        /// </summary>  
        /// <param name="servicesRegister">Register the ServiceCollection's delegate at startup</param>  
        /// <param name="initSeedData">Initialize the delegate for seed data</param>  
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

            #region Populate seed data//Preparation before execution
            var dataList = seedDataBuilder?.ExecuteAsync(this._serviceProvider).GetAwaiter().GetResult();

            dataList ??= new DataList(Guid.Empty.ToString("N"));

            if (!GlobalDataListCollection.ContainsKey(dataList.UUID))
            {
                GlobalDataListCollection[dataList.UUID] = dataList;
                GlobalDataList.AddRange(dataList);

                //autofill
                AutoFillSeedData(seedDataBuilder, dataList);
                //After filling
                seedDataBuilder?.OnExecutedAsync(this._serviceProvider, dataList).GetAwaiter().GetResult();
            }

            #endregion
        }

        private void AutoFillSeedData(UnitTestSeedDataBuilder seedDataBuilder, DataList dataLists)
        {
            using (var scope = this._serviceProvider.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;
                var dbContext = serviceProvider.GetService<NcfUnitTestEntities>();
                //Populate all data
                foreach (var dataListKv in dataLists)
                {
                    var type = dataListKv.Key;
                    var dataList = dataListKv.Value;

                    //SetupDbSet<T>
                    var binder = Type.DefaultBinder;
                    var method = dbContext.GetType().GetMethod(nameof(dbContext.Set), BindingFlags.Instance | BindingFlags.Public, binder, Type.EmptyTypes, null).MakeGenericMethod(type);
                    var dbSet = method.Invoke(dbContext, null);

                    //Convert data type
                    var listType = typeof(List<>).MakeGenericType(type);
                    var castMethod = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(type);
                    var toListMethod = typeof(Enumerable).GetMethod("ToList").MakeGenericMethod(type);

                    var castData = castMethod.Invoke(null, new object[] { dataList });
                    var listData = toListMethod.Invoke(null, new object[] { castData });

                    //Add to DbSet<T>
                    var addRangeMethod = dbSet.GetType().GetMethod("AddRange", new[] { listType });
                    addRangeMethod.Invoke(dbSet, new[] { listData });

                    dbContext.SaveChanges();
                }
            }
        }

        protected virtual void BeforeRegisterServiceCollection(IServiceCollection services)
        {
            Console.WriteLine("BaseNcfUnitTest.BeforeRegisterServiceCollection");
        }

        protected virtual void RegisterServiceCollectionFinished(IServiceCollection services)
        {

        }

        /// <summary>  
        /// Attempts to initialize seed data of the specified type
        /// </summary>  
        /// <typeparam name="T">Entity type</typeparam>  
        public void TryInitSeedData<T>() where T : EntityBase
        {
            if (!GlobalDataList.ContainsKey(typeof(T)))
            {
                GlobalDataList[typeof(T)] = new List<object>();
            }
        }



        public void BuildServiceProvider(IServiceCollection services = null)
        {
            if (services != null)
            {
                this.ServiceCollection = services;
            }
            this._serviceProvider = this.ServiceCollection.BuildServiceProvider();

        }

        protected virtual void ActionInServiceCollection()
        {

        }

        /// <summary>
        /// Register IServiceCollection and MemoryCache
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
            ServiceCollection.AddMemoryCache();//Use memory cache

            ActionInServiceCollection();

            //Set the unit test default DbContext (needs before StartNcfEngine)
            MultipleDatabasePool.UnitTestPillarDbContext = typeof(NcfUnitTestEntities);

            var result = Senparc.Ncf.XncfBase.Register.StartNcfEngine(ServiceCollection, Configuration, Env, null);

            //Override NCF basic settings
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
                //initialize object
                var options = new DbContextOptionsBuilder<NcfUnitTestEntities>()
                                    .UseInMemoryDatabase(databaseName: "UnitTestDb")
                                    //InMemory Transactions are not supported, ignore warnings if you do not want to throw errors
                                    .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                                    .Options;
                var dbContext = new NcfUnitTestEntities(options, s, GlobalDataList);

                return dbContext;

            });
        }

        /// <summary>
        /// Register RegisterService.Start()
        /// </summary>
        public void RegisterServiceStart(bool autoScanExtensionCacheStrategies = false)
        {
            //register
            registerService = Senparc.CO2NET.AspNet.RegisterServices.RegisterService.Start(Env, _senparcSetting)
                .UseSenparcGlobal(autoScanExtensionCacheStrategies);

            //Configure global use of Redis cache (on-demand, independent)
            var redisConfigurationStr = _senparcSetting.Cache_Redis_Configuration;
            var useRedis = !string.IsNullOrEmpty(redisConfigurationStr) && redisConfigurationStr != "#{Cache_Redis_Configuration}#"/*Default value, not enabled*/;
            if (useRedis)//In order to facilitate the configuration of developers in different environments, a judgment method is made here. The actual development environment is generally determined, and the if condition here can be ignored.
            {
                /* illustrate:* 1、Redis The connection string information will be retrieved from Config.SenparcSetting.Cache_Redis_Configuration Automatically obtain and register. If no modification is required, the following method can be ignored./* 2、If you need to modify it manually, you can manually set the Redis link information through the SetConfigurationOption method below (only modify the configuration, not enable it immediately)*/
                Senparc.CO2NET.Cache.CsRedis.Register.SetConfigurationOption(redisConfigurationStr);
                Console.WriteLine("Complete Redis setup");

                //The following will immediately set the global cache to Redis
                Senparc.CO2NET.Cache.CsRedis.Register.UseKeyValueRedisNow();//Key-value pair caching strategy (recommended)
                Console.WriteLine("Enable Redis UseKeyValue policy");

                //Senparc.CO2NET.Cache.Redis.Register.UseHashRedisNow();//HashSetStorage format caching strategy

                //You can also customize the caching strategy that currently needs to be enabled in the following ways
                //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisObjectCacheStrategy.Instance);//key value pair
                //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisHashSetObjectCacheStrategy.Instance);//HashSet
            }

        }
    }
}
