using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.AssembleScan;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.SqlServer;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Text;

namespace _5990_Senparc.Xncf.TenantTests
{
    [TestClass]
    public class XncfTestBase
    {
        protected static IServiceCollection ServiceCollection { get; } = new ServiceCollection();
        public static IConfiguration Configuration { get; set; }


        public static IHostEnvironment Env { get; set; }

        protected static IRegisterService registerService;
        protected static SenparcSetting _senparcSetting;

        public XncfTestBase()
        {
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            RegisterServiceCollection();
            RegisterServiceStart();
        }

        /// <summary>
        /// 注册 IServiceCollection 和 MemoryCache
        /// </summary>
        public static void RegisterServiceCollection()
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile("appsettings.json", false, false);
            var config = configBuilder.Build();
            Configuration = config;

            _senparcSetting = new SenparcSetting() { IsDebug = true };
            config.GetSection("SenparcSetting").Bind(_senparcSetting);

            ServiceCollection.AddSenparcGlobalServices(config);
            ServiceCollection.AddMemoryCache();//使用内存缓存

            //NCF 运行注册
            //已经添加完所有程序集自动扫描的委托，立即执行扫描（必须）
            AssembleScanHelper.RunScan();
            ServiceCollection.AddHttpContextAccessor();
            //激活 Xncf 扩展引擎（必须）
            ServiceCollection.StartEngine(Configuration, Env);

            ServiceCollection.ScanAssamblesForAutoDI();

            //指定数据库类型（可选），默认为 SQLiteMemoryDatabaseConfiguration
            ServiceCollection.AddDatabase<SQLServerDatabaseConfiguration>();
        }

        /// <summary>
        /// 注册 RegisterService.Start()
        /// </summary>
        public static void RegisterServiceStart(bool autoScanExtensionCacheStrategies = false)
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

            //NCF 运行
            //XncfModules（必须）
            using (var scope = ServiceCollection.BuildServiceProvider().CreateScope())
            {
                IApplicationBuilder app = new ApplicationBuilder(scope.ServiceProvider);
                Senparc.Ncf.XncfBase.Register.UseXncfModules(app, registerService);
            }
        }
    }
}
