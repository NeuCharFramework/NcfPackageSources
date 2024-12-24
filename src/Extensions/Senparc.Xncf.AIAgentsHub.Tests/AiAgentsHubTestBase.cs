using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Senparc.CO2NET.AspNet;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Tests;
using Senparc.Ncf.Database.Sqlite;
using Senparc.Xncf.AreasBase;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.AIAgentsHub.Domain.Services;
using Senparc.CO2NET.RegisterServices;
using Microsoft.Extensions.Configuration;
using Moq;
using Senparc.Xncf.AIAgentsHub.Tests.InstallServices;
using Senparc.Ncf.Core.Config;
using System.Configuration;
using Senparc.CO2NET.Extensions;
using Senparc.Xncf.SystemCore.Domain.Database;
using Senparc.Ncf.Database.SqlServer;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using Senparc.Xncf.SystemManager.Domain.Service;
using Senparc.Ncf.UnitTestExtension;
using Senparc.Ncf.Utility.Helpers;

namespace Senparc.Xncf.AIAgentsHub.Tests
{
    public class AiAgentsHubTestBase : BaseNcfUnitTest
    {
        //public IServiceCollection ServiceCollection { get; set; }
        //public IConfiguration Configuration { get; set; }

        //public IHostEnvironment Env { get; set; }

        protected IRegisterService registerService;
        //protected SenparcSetting _senparcSetting;
        protected IServiceProvider _serviceProvider;

        private WebApplicationBuilder builder;


        public AiAgentsHubTestBase()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            builder = WebApplication.CreateBuilder();

            {
                var initRegister = new Senparc.Xncf.XncfModuleManager.Register();
            }


            //添加（注册） Ncf 服务（必须）
            //builder.AddNcf<SqliteMemoryDatabaseConfiguration>();
            builder.AddNcf<SqlServerDatabaseConfiguration>();
            builder.Services.AddMemoryCache();//使用内存缓存

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            this._serviceProvider = app.Services;

            //Use NCF（必须）
            registerService = app.UseNcf();

            /*
            //初始化安装
            var installService = this._serviceProvider.GetRequiredService<InstallerService>();
            var installOptionsService = this._serviceProvider.GetRequiredService<InstallOptionsService>();
            InstallRequestDto installRequestDto = new()
            {
                AdminUserName = "Test",
                SystemName = "UnitTest",
                DbConnectionString = installOptionsService.GetDbConnectionString()
            };

            installService.InstallAsync(installRequestDto, this._serviceProvider).GetAwaiter();

            var xncfModuleService = this._serviceProvider.GetRequiredService<XncfModuleService>();

            PagedList<XncfModule> xncfModules = xncfModuleService.GetObjectListAsync(1, 999, (XncfModule z) => true, (XncfModule z) => z.AddTime, OrderingType.Descending).GetAwaiter().GetResult();
            List<CreateOrUpdate_XncfModuleDto> xncfModuleDtos = xncfModules.Select((XncfModule z) => xncfModuleService.Mapper.Map<CreateOrUpdate_XncfModuleDto>(z)).ToList();

            var installResult = Senparc.Ncf.XncfBase.Register.ScanAndInstall(xncfModuleDtos, this._serviceProvider).GetAwaiter().GetResult();
            Console.WriteLine(installResult);

            //输出调试信息
            var allKeys = Senparc.Ncf.Core.Models.EntitySetKeys.GetAllEntitySetInfo();
            Console.WriteLine("All Keys:\r\n" + allKeys.ToJson(true));
            
             */

            //安装模块
            var register = new Register();

            //安装
            register.InstallOrUpdateAsync(this._serviceProvider, Ncf.Core.Enums.InstallOrUpdate.Install).GetAwaiter();
        }
    }

    public static class TestBaseExtension
    {
        private static DateTimeOffset StartTime = SystemTime.Now;

        public static void AddNcf<TDatabaseConfiguration>(this WebApplicationBuilder builder)
          where TDatabaseConfiguration : IDatabaseConfiguration, new()
        {
            var services = builder.Services;
            SenparcDI.GlobalServiceCollection = services;


            services.AddMemoryCache();//使用内存缓存

            services.AddScoped<InstallOptionsService>();
            services.AddScoped<InstallerService>();
            services.AddScoped<Senparc.Ncf.Repository.RepositoryBase<Color>>();
            services.AddScoped<INcfDbData, NcfClientDbData>();

            services.AddScoped<SystemConfigService>();

            services.AddScoped<ColorService>();

            //激活 Xncf 扩展引擎（必须）
            var logMsg = builder.StartWebEngine(null);
            Console.WriteLine("============ logMsg =============");
            Console.WriteLine(logMsg);
            Console.WriteLine("============ logMsg END =============");
        }

        public static IRegisterService UseNcf(this WebApplication app)
        {
            //注册
            //var mockEnv = new Mock<IHostEnvironment/*IHostingEnvironment*/>();
            //mockEnv.Setup(z => z.ContentRootPath).Returns(() => UnitTestHelper.RootPath);
            //var env = mockEnv.Object;

            var env = app.Environment;

            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile("appsettings.json", false, false);
            Console.WriteLine("完成 appsettings.json 添加");

            var config = configBuilder.Build();
            Console.WriteLine("完成 ServiceCollection 和 ConfigurationBuilder 初始化");

            //更多绑定操作参见：https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-2.2
            var senparcSetting = new SenparcSetting();
            config.GetSection("SenparcSetting").Bind(senparcSetting);

            var senparcCoreSetting = new SenparcCoreSetting();
            config.GetSection("SenparcCoreSetting").Bind(senparcCoreSetting);

            Console.WriteLine("SenparcCoreSetting：");
            Console.WriteLine(senparcCoreSetting.ToJson(true));
            Console.WriteLine();

            // 启动 CO2NET 全局注册，必须！
            // 关于 UseSenparcGlobal() 的更多用法见 CO2NET Demo：https://github.com/Senparc/Senparc.CO2NET/blob/master/Sample/Senparc.CO2NET.Sample.netcore3/Startup.cs
            var registerService = app
                //全局注册
                .UseSenparcGlobal(env, senparcSetting, globalRegister =>
                {
                    //配置全局使用Redis缓存（按需，独立）
                    if (UseRedis(senparcSetting, out var redisConfigurationStr))//这里为了方便不同环境的开发者进行配置，做成了判断的方式，实际开发环境一般是确定的，这里的if条件可以忽略
                    {
                        /* 说明：
                         * 1、Redis 的连接字符串信息会从 Config.SenparcSetting.Cache_Redis_Configuration 自动获取并注册，如不需要修改，下方方法可以忽略
                        /* 2、如需手动修改，可以通过下方 SetConfigurationOption 方法手动设置 Redis 链接信息（仅修改配置，不立即启用）
                         */
                        CO2NET.Cache.CsRedis.Register.SetConfigurationOption(redisConfigurationStr);

                        //以下会立即将全局缓存设置为 Redis
                        CO2NET.Cache.CsRedis.Register.UseKeyValueRedisNow(); //键值对缓存策略（推荐）
                        /*Senparc.CO2NET.Cache.CsRedis.Register.UseHashRedisNow();*/ //HashSet储存格式的缓存策略 

                        //也可以通过以下方式自定义当前需要启用的缓存策略
                        //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisObjectCacheStrategy.Instance);//键值对
                        //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisHashSetObjectCacheStrategy.Instance);//HashSet
                    }
                    //如果这里不进行Redis缓存启用，则目前还是默认使用内存缓存 
                });


            //XncfModules（必须）
            app.UseXncfModules(registerService, senparcCoreSetting);

            return registerService;
        }

        /// <summary>
        /// 判断当前配置是否满足使用 Redis（根据是否已经修改了默认配置字符串判断）
        /// </summary>
        /// <param name="senparcSetting"></param>
        /// <returns></returns>
        internal static bool UseRedis(SenparcSetting senparcSetting, out string redisConfigurationStr)
        {
            redisConfigurationStr = senparcSetting.Cache_Redis_Configuration;
            var useRedis = !string.IsNullOrEmpty(redisConfigurationStr) && redisConfigurationStr != "#{Cache_Redis_Configuration}#"/*默认值，不启用*/;
            return useRedis;
        }

        /// <summary>
        /// 输出启动成功标志
        /// </summary>
        /// <param name="app"></param>
        public static void ShowSuccessTip(this WebApplication app)
        {
            //输出启动成功标志
            var ready = "";
            var cost = "";

            GlobalCulture.Create()
               .SetEnglish(() =>
               {
                   ready = "The start-up is ready";
                   cost = "Cost Time:";
               })
               .SetChinese(() =>
               {
                   ready = "启动工作准备就绪";
                   cost = "用时：";
               })
               .InvokeDefault();

            Senparc.Ncf.Core.VersionManager.ShowSuccessTip($"\t\t{ready}\r\n\t\t{cost}{SystemTime.NowDiff(StartTime).TotalSeconds} s");
        }
    }
}
