using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Repository;
using Senparc.Ncf.UnitTestExtension.Database;
using Senparc.Ncf.UnitTestExtension.Entities;
using Senparc.Ncf.Utility.ExpressionExtension;
using Senparc.Xncf.SystemCore.Domain.Database;

namespace Senparc.Ncf.UnitTestExtension
{
    [TestClass]
    public class BaseNcfUnitTest
    {
        public IServiceCollection ServiceCollection { get; set; }
        public IConfiguration Configuration { get; set; }

        public IHostEnvironment Env { get; set; }

        protected IRegisterService registerService;
        protected SenparcSetting _senparcSetting;

        protected IServiceProvider _serviceProvider;
        protected DataList dataLists = new DataList();

        public BaseNcfUnitTest() : this(null, null)
        {

        }

        /// <summary>  
        /// 构造函数，用于初始化服务提供者和种子数据  
        /// </summary>  
        /// <param name="servicesRegister">在启动时注册 ServiceCollection 的委托</param>  
        /// <param name="initSeedData">初始化种子数据的委托</param>  
        public BaseNcfUnitTest(Action<IServiceCollection> servicesRegister = null, Action<DataList> initSeedData = null)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            ServiceCollection = new ServiceCollection();

            initSeedData?.Invoke(dataLists);
            servicesRegister?.Invoke(ServiceCollection);

            RegisterServiceCollection();
            RegisterServiceStart();
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

        public (IClientRepositoryBase<T> Repository, List<T> DataList) GetRespositoryBase<T>() where T : EntityBase
        {
            var dataList = dataLists[typeof(T)].Cast<T>().ToList();

            //var mockRepository = new Mock<IClientRepositoryBase<T>>();

            //设置 RepositoryBase.BaseDB.ManualDetectChangeObject
            var mockBaseDB = new Mock<NcfUnitTestDataDb>();
            //mockBaseDB.Setup(z => z.ManualDetectChangeObject).Returns(true);


            mockBaseDB.Setup(z => z.DataContext.Set<T>())
                      .Returns<DbSet<T>>(z =>
                      {
                          var mockDbSet = UnitTestHelper.CreateMockDbSet(dataList);
                          return mockDbSet.Object;
                      });

            var repo = new ClientRepositoryBase<T>(mockBaseDB.Object);

            return (Repository: repo, DataList: dataList);
        }

        /// <summary>  
        /// 获取指定类型的仓储实例，带有预设的 Mock 行为  
        /// </summary>  
        /// <typeparam name="T">实体类型</typeparam>  
        /// <returns>仓储实例</returns>  
        public (Mock<IClientRepositoryBase<T>> MockRepository, List<T> DataList) GetRespository<T, TKey>() where T : EntityBase<TKey>
        {
            var repo = GetRespository<T>();

            var dataList = repo.DataList;

            // Mock Update 方法  
            repo.MockRepository.Setup(z => z.Update(It.IsAny<T>()))
                .Callback<T>(obj =>
                {
                    var existing = dataList.FirstOrDefault(o => o == obj);
                    if (existing != null)
                    {
                        var index = dataList.IndexOf(existing);
                        dataList[index] = obj;
                    }
                });

            // Mock SaveAsync 方法  
            repo.MockRepository.Setup(z => z.SaveAsync(It.IsAny<T>()))
                 .Returns<T>(obj =>
                 {
                     var existing = dataList.FirstOrDefault(o => o == obj);
                     if (existing != null)
                     {
                         var index = dataList.IndexOf(existing);
                         dataList[index] = obj;
                     }
                     else
                     {
                         dataList.Add(obj);
                     }
                     return Task.CompletedTask;
                 });

            return repo;
        }

        public (Mock<IClientRepositoryBase<T>> MockRepository, List<T> DataList) GetRespository<T, TOrderProperty, TIncludesProperty>() where T : EntityBase
        {
            TryInitSeedData<T>();

            var dataList = dataLists[typeof(T)].Cast<T>().ToList();

            var mockRepository = new Mock<IClientRepositoryBase<T>>();

            //设置 RepositoryBase.BaseDB.ManualDetectChangeObject
            var mockBaseDB = new Mock<NcfUnitTestDataDb>();
            //mockBaseDB.Setup(z => z.ManualDetectChangeObject).Returns(true);


            mockBaseDB.Setup(z => z.DataContext.Set<T>())
                      .Returns<DbSet<T>>(z =>
                      {
                          var mockDbSet = UnitTestHelper.CreateMockDbSet(dataList);
                          return mockDbSet.Object;
                      });


            mockBaseDB.SetupProperty(z => z.ManualDetectChangeObject, true);
            mockRepository.Setup(z => z.BaseDB).Returns(mockBaseDB.Object);


            // Mock GetFirstOrDefaultObjectAsync 方法  
            mockRepository.Setup(z => z.GetFirstOrDefaultObjectAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<string[]>()))
                .Returns<Expression<Func<T, bool>>, string[]>((expr, includes) =>
                {
                    var func = expr.Compile();
                    return Task.FromResult(dataList.FirstOrDefault(func));
                });

            // Mock GetFirstOrDefaultObject 方法  
            mockRepository.Setup(z => z.GetFirstOrDefaultObject(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<string[]>()))
                .Returns<Expression<Func<T, bool>>, string[]>((expr, includes) =>
                {
                    var func = expr.Compile();
                    return dataList.FirstOrDefault(func);
                });

            // Mock GeAll 方法  
            mockRepository.Setup(z => z.GeAll(It.IsAny<Expression<Func<T, object>>>(), It.IsAny<OrderingType>(), It.IsAny<string[]>()))
                .Returns<Expression<Func<T, object>>, OrderingType, string[]>((orderBy, orderingType, includes) =>
                {
                    return dataList.AsQueryable().OrderBy(orderBy);
                });

            // Mock ObjectCount 方法  
            mockRepository.Setup(z => z.ObjectCount(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<string[]>()))
                .Returns<Expression<Func<T, bool>>, string[]>((where, includes) =>
                {
                    var func = where.Compile();
                    return dataList.Count(func);
                });

            // Mock Add 方法  
            mockRepository.Setup(z => z.Add(It.IsAny<T>()))
                .Callback<T>(obj =>
                {
                    dataList.Add(obj);
                });

            // Mock Delete 方法  
            mockRepository.Setup(z => z.Delete(It.IsAny<T>(), It.IsAny<bool>()))
                .Callback<T, bool>((obj, softDelete) =>
                {
                    dataList.Remove(obj);
                });

            // Mock SaveChanges 方法  
            mockRepository.Setup(z => z.SaveChanges())
                .Callback(() => { /* No-op for in-memory data */ });

            // Mock DeleteAsync 方法  
            mockRepository.Setup(z => z.DeleteAsync(It.IsAny<T>(), It.IsAny<bool>()))
                .Returns<T, bool>((obj, softDelete) =>
                {
                    dataList.Remove(obj);
                    return Task.CompletedTask;
                });

            // Mock SaveChangesAsync 方法  
            mockRepository.Setup(z => z.SaveChangesAsync())
                .Returns(Task.CompletedTask);


            //// 泛型版本的Mock设置  
            //mockRepository.Setup(z => z.GetObjectListAsync<TOrderProperty>(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<Expression<Func<T, TOrderProperty>>>(), It.IsAny<OrderingType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string[]>()))
            //    .Returns<Expression<Func<T, bool>>, Expression<Func<T, TOrderProperty>>, OrderingType, int, int, string[]>((where, orderBy, orderingType, pageIndex, pageCount, includes) =>
            //    {
            //        Console.WriteLine("GetObjectListAsync run with TOrderProperty:" + typeof(T).Name);
            //        var func = where.Compile();
            //        var orderedData = orderingType == OrderingType.Ascending ?
            //            dataList.AsQueryable().Where(where).OrderBy(orderBy) :
            //            dataList.AsQueryable().Where(where).OrderByDescending(orderBy);
            //        return Task.FromResult(new PagedList<T>(orderedData.Skip((pageIndex - 1) * pageCount).Take(pageCount).ToList(), pageIndex, pageCount, dataList.Count));
            //    });


            // Mock GetObjectListAsync 方法
            mockRepository.Setup(z => z.GetObjectListAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<Expression<Func<T, TOrderProperty>>>(), It.IsAny<OrderingType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string[]>()))
                .Returns<Expression<Func<T, bool>>, Expression<Func<T, int>>, OrderingType, int, int, string[]>((where, orderBy, orderingType, pageIndex, pageCount, includes) =>
                {
                    var func = where.Compile();
                    var orderedData = orderingType == OrderingType.Ascending ?
                        dataList.AsQueryable().Where(where).OrderBy(orderBy) :
                        dataList.AsQueryable().Where(where).OrderByDescending(orderBy);

                    var result = pageIndex <= 0 && pageCount <= 0
                        ? orderedData.ToList()
                        : orderedData.Skip((pageIndex - 1) * pageCount).Take(pageCount).ToList();

                    return Task.FromResult(new PagedList<T>(result, pageIndex, pageCount, dataList.Count));
                });

            //GetObjectListAsync(Expression<Func<T, bool>> where, string OrderbyField, int pageIndex, int pageCount, params string[] includes)
            mockRepository.Setup(repo => repo.GetObjectListAsync(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string[]>()))
            .Returns<Expression<Func<T, bool>>, string, int, int, string[]>((where, OrderbyField, pageIndex, pageCount, includes) =>
            {
                var func = where.Compile();
                var orderedData = dataList.AsQueryable().Where(where).OrderByExtension(OrderbyField);

                var result = pageIndex <= 0 && pageCount <= 0
                    ? orderedData.ToList()
                    : orderedData.Skip((pageIndex - 1) * pageCount).Take(pageCount).ToList();

                return Task.FromResult(new PagedList<T>(result, pageIndex, pageCount, dataList.Count));
            }); ;

            // Mock GetFirstOrDefaultObjectAsync 方法 (带 includesNavigationPropertyPathFunc)  
            mockRepository.Setup(z => z.GetFirstOrDefaultObjectAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>>>()))
                .Returns<Expression<Func<T, bool>>, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>>>((where, includesNavigationPropertyPathFunc) =>
                {
                    //TODO:待完善
                    var func = where.Compile();
                    return Task.FromResult(dataList.FirstOrDefault(func));
                });

            mockRepository.Setup(z => z.GetFirstOrDefaultObjectAsync(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<Expression<Func<T, TOrderProperty>>>(),
                It.IsAny<OrderingType>(),
                It.IsAny<Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>>>()))
            .Returns<Expression<Func<T, bool>>, Expression<Func<T, TOrderProperty>>, OrderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>>>(
                async (where, orderBy, orderingType, includesNavigationPropertyPathFunc) =>
                {
                    var mockDbSet = UnitTestHelper.CreateMockDbSet(dataList);

                    // 使用Mock DbSet  
                    var query = await includesNavigationPropertyPathFunc.Compile()(mockDbSet.Object)
                                        .OrderBy(orderBy, orderingType)
                                        .Where(where)
                                        .OrderBy(orderBy, orderingType).FirstOrDefaultAsync();

                    return query;
                });


            // Mock ObjectCountAsync 方法  
            mockRepository.Setup(z => z.ObjectCountAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<string[]>()))
                .Returns<Expression<Func<T, bool>>, string[]>((where, includes) =>
                {
                    var func = where.Compile();
                    return Task.FromResult(dataList.Count(func));
                });

            return (MockRepository: mockRepository, DataList: dataList);
        }

        /// <summary>  
        /// 获取指定类型的仓储实例，带有预设的 Mock 行为  
        /// </summary>  
        /// <typeparam name="T">实体类型（OrderBy 默认为 int 类型，include 默认为 object 类型）</typeparam>  
        /// <returns>仓储实例</returns>  
        public (Mock<IClientRepositoryBase<T>> MockRepository, List<T> DataList) GetRespository<T>() where T : EntityBase
        {
            var result = this.GetRespository<T, int, object>();
            return result;
        }

        /// <summary>
        /// 获取指定类型的仓储实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IClientRepositoryBase<T> GetRespositoryObject<T>() where T : EntityBase
        {
            return this.GetRespository<T>().MockRepository.Object;
        }

        public Mock<TInterface> CreateMockForExtendedInterface<TInterface, TBase>(Mock<TBase> baseMock)
            where TInterface : class, TBase
            where TBase : class
        {
            return baseMock.As<TInterface>();
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
            ServiceCollection = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(UnitTestHelper.GetAppSettingsFile(), false, false);
            var config = configBuilder.Build();
            Configuration = config;

            _senparcSetting = new SenparcSetting() { IsDebug = true };
            config.GetSection("SenparcSetting").Bind(_senparcSetting);

            ServiceCollection.AddSenparcGlobalServices(config);
            ServiceCollection.AddMemoryCache();//使用内存缓存

            ActionInServiceCollection();

            var result = Senparc.Ncf.XncfBase.Register.StartNcfEngine(ServiceCollection, Configuration, Env, null);

            //覆盖 NCF 基础设置

            //BasePoolEntities 工厂配置（上层应用实际不会用到，构建 NcfClientDbData 时需要）
            Func<IServiceProvider, BasePoolEntities> basePoolEntitiesImplementationFactory = s =>
            {
                var multipleDatabasePool = MultipleDatabasePool.Instance;
                return multipleDatabasePool.GetXncfDbContext(this.GetType(), serviceProvider: s) as BasePoolEntities;
            };
            ServiceCollection.AddScoped<BasePoolEntities>(basePoolEntitiesImplementationFactory);


            //ServiceCollection.AddScoped<NcfUnitTestDataDb>();
            //ServiceCollection.AddScoped<NcfUnitTestEntities>(s =>
            //{
            //    var mockDbContext = new Mock<NcfUnitTestEntities>(new DbContextOptions<NcfUnitTestEntities>(), s);
            //    //var dbContext = new NcfUnitTestEntities(new DbContextOptions<DbContext>(), s);

            //    // 获取 DbContext 的所有属性  
            //    var properties = typeof(NcfUnitTestEntities).GetProperties(BindingFlags.Public | BindingFlags.Instance);


            //    mockDbContext.Setup(z => z.Set<It.IsAnyType>())
            //          .Returns<DbSet<It.IsAnyType>>(z =>
            //          {
            //              var properties = z.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);



            //              var dataList = dataLists[].
            //              var mockDbSet = UnitTestHelper.CreateMockDbSet(dataList);
            //              return mockDbSet.Object;
            //          });
            //});

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


            BuildServiceProvider();
        }

        /// <summary>
        /// 注册 RegisterService.Start()
        /// </summary>
        public void RegisterServiceStart(bool autoScanExtensionCacheStrategies = false)
        {
            //注册
            var mockEnv = new Mock<IHostEnvironment/*IHostingEnvironment*/>();
            mockEnv.Setup(z => z.ContentRootPath).Returns(() => UnitTestHelper.RootPath);

            Env = mockEnv.Object;

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
