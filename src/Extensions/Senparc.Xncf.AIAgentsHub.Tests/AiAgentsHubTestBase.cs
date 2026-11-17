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


            //Legacy comment translated to English.
            //builder.AddNcf<SqliteMemoryDatabaseConfiguration>();
            builder.AddNcf<SqlServerDatabaseConfiguration>();
            builder.Services.AddMemoryCache();//Legacy comment translated to English.

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            this._serviceProvider = app.Services;

            //Use NCF（must）
            registerService = app.UseNcf();

            /*

            Legacy comment translated to English.

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



            //outputdebug information

            var allKeys = Senparc.Ncf.Core.Models.EntitySetKeys.GetAllEntitySetInfo();

            Console.WriteLine("All Keys:\r\n" + allKeys.ToJson(true));



             */

            //Legacy comment translated to English.
            var register = new Register();

            //Legacy comment translated to English.
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


            services.AddMemoryCache();//Legacy comment translated to English.

            services.AddScoped<InstallOptionsService>();
            services.AddScoped<InstallerService>();
            services.AddScoped<Senparc.Ncf.Repository.RepositoryBase<Color>>();
            services.AddScoped<INcfDbData, NcfClientDbData>();

            services.AddScoped<SystemConfigService>();

            services.AddScoped<ColorService>();

            //Legacy comment translated to English.
            var logMsg = builder.StartWebEngine(null);
            Console.WriteLine("============ logMsg =============");
            Console.WriteLine(logMsg);
            Console.WriteLine("============ logMsg END =============");
        }

        public static IRegisterService UseNcf(this WebApplication app)
        {
            //Legacy comment translated to English.
            //var mockEnv = new Mock<IHostEnvironment/*IHostingEnvironment*/>();
            //mockEnv.Setup(z => z.ContentRootPath).Returns(() => UnitTestHelper.RootPath);
            //var env = mockEnv.Object;

            var env = app.Environment;

            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile("appsettings.json", false, false);
            Console.WriteLine("完成 appsettings.json 添加");

            var config = configBuilder.Build();
            Console.WriteLine("完成 ServiceCollection 和 ConfigurationBuilder 初始化");

            //Legacy comment translated to English.
            var senparcSetting = new SenparcSetting();
            config.GetSection("SenparcSetting").Bind(senparcSetting);

            var senparcCoreSetting = new SenparcCoreSetting();
            config.GetSection("SenparcCoreSetting").Bind(senparcCoreSetting);

            Console.WriteLine("SenparcCoreSetting：");
            Console.WriteLine(senparcCoreSetting.ToJson(true));
            Console.WriteLine();

            // start CO2NET global registration，must！
            //Legacy comment translated to English.
            var registerService = app
                //global registration
                .UseSenparcGlobal(env, senparcSetting, globalRegister =>
                {
                    //Legacy comment translated to English.
                    if (UseRedis(senparcSetting, out var redisConfigurationStr))//Legacy comment translated to English.
                    {
                        /* description：

                         * Legacy comment translated to English.

                        Legacy comment translated to English.

                         */
                        CO2NET.Cache.CsRedis.Register.SetConfigurationOption(redisConfigurationStr);

                        //Legacy comment translated to English.
                        CO2NET.Cache.CsRedis.Register.UseKeyValueRedisNow(); //Legacy comment translated to English.
                        /*Senparc.CO2NET.Cache.CsRedis.Register.UseHashRedisNow();*/ //Legacy comment translated to English.

                        //Legacy comment translated to English.
                        //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisObjectCacheStrategy.Instance);//key-value pair
                        //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisHashSetObjectCacheStrategy.Instance);//HashSet
                    }
                    //Legacy comment translated to English.
                });


            //XncfModules（must）
            app.UseXncfModules(registerService, senparcCoreSetting);

            return registerService;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="senparcSetting"></param>
        /// <returns></returns>
        internal static bool UseRedis(SenparcSetting senparcSetting, out string redisConfigurationStr)
        {
            redisConfigurationStr = senparcSetting.Cache_Redis_Configuration;
            var useRedis = !string.IsNullOrEmpty(redisConfigurationStr) && redisConfigurationStr != "#{Cache_Redis_Configuration}#"/*default value; disabled*/;
            return useRedis;
        }

        /// <summary>
        /// output the startup success marker
        /// </summary>
        /// <param name="app"></param>
        public static void ShowSuccessTip(this WebApplication app)
        {
            //output the startup success marker
            var ready = "";
            var startedAt = "";
            var cost = "";

            GlobalCulture.Create()
               .SetEnglish(() =>
               {
                   ready = "The start-up is ready";
                   startedAt = "Started at: ";
                   cost = "Cost Time:";
               })
               .SetChinese(() =>
               {
                   ready = "启动工作准备就绪";
                   startedAt = "启动时间：";
                   cost = "用时：";
               })
               .InvokeDefault();

            Senparc.Ncf.Core.VersionManager.ShowSuccessTip($"\t\t{ready}\r\n\t\t{startedAt}{StartTime:yyyy-MM-dd HH:mm:ss}\r\n\t\t{cost}{SystemTime.NowDiff(StartTime).TotalSeconds} s");
        }
    }
}
