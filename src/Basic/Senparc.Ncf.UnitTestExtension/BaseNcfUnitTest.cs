using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Humanizer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.Dm;
using Senparc.Ncf.Database.InMemory;
using Senparc.Ncf.Repository;
using Senparc.Ncf.UnitTestExtension.Database;
using Senparc.Ncf.UnitTestExtension.Entities;
using Senparc.Ncf.Utility.ExpressionExtension;
using Senparc.Xncf.SystemCore.Domain.Database;

namespace Senparc.Ncf.UnitTestExtension
{

    [TestClass]
    public partial class BaseNcfUnitTest
    {
        private IServiceCollection ServiceCollection { get; set; }
        public IConfiguration Configuration { get; set; }

        public IHostEnvironment Env { get; set; }

        protected IRegisterService registerService;
        protected SenparcSetting _senparcSetting;

        protected IServiceProvider _serviceProvider;
        protected DataList dataLists = new DataList();

        //public BaseNcfUnitTest() : this(null, null)
        //{

        //}

        /// <summary>  
        /// 构造函数，用于初始化服务提供者和种子数据  
        /// </summary>  
        /// <param name="servicesRegister">在启动时注册 ServiceCollection 的委托</param>  
        /// <param name="initSeedData">初始化种子数据的委托</param>  
        public BaseNcfUnitTest(Action<IServiceCollection> servicesRegister = null, Action<DataList> initSeedData = null)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var builder = WebApplication.CreateBuilder();
            ServiceCollection = builder.Services;

            var mockEnv = new Mock<IHostEnvironment/*IHostingEnvironment*/>();
            mockEnv.Setup(z => z.ContentRootPath).Returns(() => UnitTestHelper.RootPath);

            Env = mockEnv.Object;

            initSeedData?.Invoke(dataLists);
            servicesRegister?.Invoke(ServiceCollection);

            //Console.WriteLine("Test Data:" + dataLists.ToJson(true, new Newtonsoft.Json.JsonSerializerSettings()
            //{
            //    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            //}));

            BeforeRegisterServiceCollection(ServiceCollection);
            RegisterServiceCollection();
            RegisterServiceCollectionFinished(ServiceCollection);

            BuildServiceProvider();

            RegisterServiceStart();

            var app = builder.Build();
            app.UseNcfDatabase<InMemoryDatabaseConfiguration>();
        }

        protected virtual void BeforeRegisterServiceCollection(IServiceCollection services)
        {
            Console.WriteLine("BaseNcfUnitTest.BeforeRegisterServiceCollection");
        }

        protected virtual void RegisterServiceCollectionFinished(IServiceCollection services)
        {

        }

        /// <summary>  
        /// 尝试初始化指定类型的种子数据  
        /// </summary>  
        /// <typeparam name="T">实体类型</typeparam>  
        public void TryInitSeedData<T>() where T : EntityBase
        {
            if (!dataLists.ContainsKey(typeof(T)))
            {
                dataLists[typeof(T)] = new List<object>();
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
        /// 注册 IServiceCollection 和 MemoryCache
        /// </summary>
        public void RegisterServiceCollection()
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(UnitTestHelper.GetAppSettingsFile(), false, false);
            var config = configBuilder.Build();
            Configuration = config;

            _senparcSetting = new SenparcSetting() { IsDebug = true };
            config.GetSection("SenparcSetting").Bind(_senparcSetting);

            ServiceCollection.AddSenparcGlobalServices(config);
            ServiceCollection.AddMemoryCache();//使用内存缓存

            ActionInServiceCollection();

            //设置单元测试默认 DbContext（需要在 StartNcfEngine 之前）
            MultipleDatabasePool.UnitTestPillarDbContext = typeof(NcfUnitTestEntities);

            var result = Senparc.Ncf.XncfBase.Register.StartNcfEngine(ServiceCollection, Configuration, Env, null);

            //覆盖 NCF 基础设置
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
                //初始化对象
                var options = new DbContextOptionsBuilder<NcfUnitTestEntities>()
                                    .UseInMemoryDatabase(databaseName: "UnitTestDb")
                                    .Options;
                var dbContext = new NcfUnitTestEntities(options, s, dataLists);

                //填充所有数据
                foreach (var dataListKv in dataLists)
                {
                    var type = dataListKv.Key;
                    var dataList = dataListKv.Value;

                    //设置 DbSet<T>
                    var binder = Type.DefaultBinder;
                    var method = dbContext.GetType().GetMethod(nameof(dbContext.Set), BindingFlags.Instance | BindingFlags.Public, binder, Type.EmptyTypes, null).MakeGenericMethod(type);
                    var dbSet = method.Invoke(dbContext, null);

                    //转换 data 类型
                    var listType = typeof(List<>).MakeGenericType(type);
                    var castMethod = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(type);
                    var toListMethod = typeof(Enumerable).GetMethod("ToList").MakeGenericMethod(type);

                    var castData = castMethod.Invoke(null, new object[] { dataList });
                    var listData = toListMethod.Invoke(null, new object[] { castData });

                    //添加到 DbSet<T>
                    var addRangeMethod = dbSet.GetType().GetMethod("AddRange", new[] { listType });
                    addRangeMethod.Invoke(dbSet, new[] { listData });

                    dbContext.SaveChanges();
                }

                //// 获取所有DbSet属性  
                //var dbSetProperties = typeof(NcfUnitTestEntities).GetProperties().Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

                //Console.WriteLine("dbSetProperties Cunt:" + dbSetProperties.Count());

                //foreach (var dbSetProperty in dbSetProperties)
                //{
                //    // 获取DbSet的类型  
                //    var dbSetType = dbSetProperty.PropertyType.GetGenericArguments()[0];
                //    Console.WriteLine("找到 DbSet：" + dbSetType.FullName);

                //    // 创建模拟的DbSet  
                //    var mockDbSetType = typeof(Mock<>).MakeGenericType(typeof(DbSet<>).MakeGenericType(dbSetType));
                //    var mockDbSet = Activator.CreateInstance(mockDbSetType);

                //    // 设置DbSet的行为  
                //    if (dataLists.TryGetValue(dbSetType, out var data))
                //    {
                //        var queryableData = data.AsQueryable();

                //        // 设置DbSet的Provider属性  
                //        mockDbSetType.GetProperty("Provider").SetValue(mockDbSet, queryableData.Provider);

                //        // 设置DbSet的Expression属性  
                //        mockDbSetType.GetProperty("Expression").SetValue(mockDbSet, queryableData.Expression);

                //        // 设置DbSet的ElementType属性  
                //        mockDbSetType.GetProperty("ElementType").SetValue(mockDbSet, queryableData.ElementType);

                //        // 设置DbSet的GetEnumerator()方法  
                //        mockDbSetType.GetMethod("GetEnumerator").Invoke(mockDbSet, null);

                //        // 将模拟的DbSet设置到模拟的DbContext中  
                //        dbSetProperty.SetValue(dbContext, mockDbSet);
                //    }

                //}
                return dbContext;

            });

            //ServiceCollection.AddScoped(typeof(ClientRepositoryBase<>));

            //var mockBaseDB = new Mock<NcfUnitTestDataDb>();
            ////mockBaseDB.Setup(z => z.ManualDetectChangeObject).Returns(true);



            //mockBaseDB.Setup(z => z.DataContext.Set<T>())
            //          .Returns<DbSet<T>>(z =>
            //          {
            //              var mockDbSet = UnitTestHelper.CreateMockDbSet(dataList);
            //              return mockDbSet.Object;
            //          });

            //var repo = new ClientRepositoryBase<T>(mockBaseDB.Object);


        }

        /// <summary>
        /// 注册 RegisterService.Start()
        /// </summary>
        public void RegisterServiceStart(bool autoScanExtensionCacheStrategies = false)
        {
            //注册
            registerService = Senparc.CO2NET.AspNet.RegisterServices.RegisterService.Start(Env, _senparcSetting)
                .UseSenparcGlobal(autoScanExtensionCacheStrategies);

            //配置全局使用Redis缓存（按需，独立）
            var redisConfigurationStr = _senparcSetting.Cache_Redis_Configuration;
            var useRedis = !string.IsNullOrEmpty(redisConfigurationStr) && redisConfigurationStr != "#{Cache_Redis_Configuration}#"/*默认值，不启用*/;
            if (useRedis)//这里为了方便不同环境的开发者进行配置，做成了判断的方式，实际开发环境一般是确定的，这里的if条件可以忽略
            {
                /* 说明：
                 * 1、Redis 的连接字符串信息会从 Config.SenparcSetting.Cache_Redis_Configuration 自动获取并注册，如不需要修改，下方方法可以忽略
                /* 2、如需手动修改，可以通过下方 SetConfigurationOption 方法手动设置 Redis 链接信息（仅修改配置，不立即启用）
                 */
                Senparc.CO2NET.Cache.Redis.Register.SetConfigurationOption(redisConfigurationStr);
                Console.WriteLine("完成 Redis 设置");

                //以下会立即将全局缓存设置为 Redis
                Senparc.CO2NET.Cache.Redis.Register.UseKeyValueRedisNow();//键值对缓存策略（推荐）
                Console.WriteLine("启用 Redis UseKeyValue 策略");

                //Senparc.CO2NET.Cache.Redis.Register.UseHashRedisNow();//HashSet储存格式的缓存策略

                //也可以通过以下方式自定义当前需要启用的缓存策略
                //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisObjectCacheStrategy.Instance);//键值对
                //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisHashSetObjectCacheStrategy.Instance);//HashSet
            }

        }
    }
}
