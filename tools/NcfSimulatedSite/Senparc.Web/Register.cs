using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.CO2NET;
using Senparc.CO2NET.AspNet;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.AreasBase;
using Senparc.Ncf.Core.EventBus;

namespace Senparc.Web
{
    /// <summary>
    /// Global registration
    /// </summary>
    public static class Register
    {
        private static System.DateTime StartTime = SystemTime.Now.DateTime;

        public static void AddNcf(this WebApplicationBuilder builder)
        {
            StartTime = SystemTime.Now.DateTime;

            //Activate Xncf extension engine (required)
            var logMsg = builder.StartWebEngine(new[] { "Senparc.Areas.Admin"});
            //If you don't need to enable Areas, you can just use the services.StartEngine() or services.StartEngine() method

            Console.WriteLine("============ logMsg =============");
            Console.WriteLine(logMsg);
            Console.WriteLine("============ logMsg END =============");
            
            // Register EventBus and automatically scan all modules for EventHandler
            // Must be after StartWebEngine to ensure all module assemblies are loaded
            var assembliesToScan = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && 
                           (a.FullName.Contains("Senparc.Xncf.") || 
                            a.FullName.Contains("Senparc.Areas.")))
                .ToArray();
            
            Console.WriteLine($"EventBus 扫描程序集:");
            foreach (var asm in assembliesToScan)
            {
                Console.WriteLine($"  - {asm.GetName().Name}");
            }
            
            builder.Services.AddSenparcEventBus(
                options =>
                {
                    options.MaxConcurrency = Math.Max(4, Environment.ProcessorCount * 2);
                    options.EnableDuplicateDetection = true;
                    options.RetryOnFailure = true;
                    options.MaxRetryAttempts = 3;
                    options.MaxEventChainDepth = 10;
                    options.EnableCircularReferenceDetection = true;
                },
                assembliesToScan);
            
            Console.WriteLine($"EventBus 已注册，共扫描了 {assembliesToScan.Length} 个程序集");

            Console.WriteLine("============ logMsg =============");
            Console.WriteLine(logMsg);
            Console.WriteLine("============ logMsg END =============");

            #region 仅在完全删除 Senparc.Xncf.Swagger 时启用以下代码！

            // If Senparc.Xncf.Swagger is not referenced in the project, you need to use the following code to manually enable DynamicAPI. For more examples reference:
            // https://github.com/Senparc/Senparc.CO2NET/blob/master/Sample/Senparc.CO2NET.Sample.net7/Startup.cs

            //var services = builder.Services;
            //var mvcBuilder = services.AddMvcCore();
            //services.AddAndInitDynamicApi(mvcBuilder, options =>
            //{
            //    options.DefaultRequestMethod = ApiRequestMethod.Get;
            //    options.BaseApiControllerType = null;
            //    options.CopyCustomAttributes = true;
            //    options.TaskCount = Environment.ProcessorCount * 10;
            //    options.ShowDetailApiLog = true;
            //    options.AdditionalAttributeFunc = null;
            //    options.ForbiddenExternalAccess = false;
            //    options.UseLowerCaseApiName = true;
            //});

            #endregion


            //If running in IIS, you need to add IIS configuration
            //https://docs.microsoft.com/zh-cn/aspnet/core/host-and-deploy/iis/index?view=aspnetcore-2.1&tabs=aspnetcore2x#supported-operating-systems
            //services.Configure<IISOptions>(options =>
            //{
            //    options.ForwardClientCertificate = false;
            //});

            //Enable the following code to force https access
            //services.AddHttpsRedirection(options =>
            //{
            //    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
            //    options.HttpsPort = 443;
            //});
        }

        public static void UseNcf<TDatabaseConfiguration>(this WebApplication app)
            where TDatabaseConfiguration : IDatabaseConfiguration, new()
        {
            //Inject DI object
            app.UseSenparcMvcDI();

            IWebHostEnvironment env = app.Environment;
            IOptions<SenparcSetting> senparcSetting = app.Services.GetService<IOptions<SenparcSetting>>();
            IOptions<SenparcCoreSetting> senparcCoreSetting = app.Services.GetService<IOptions<SenparcCoreSetting>>();

            // Start CO2NET global registration, must!
            // For more usage of UseSenparcGlobal(), see CO2NET Demo: https://github.com/Senparc/Senparc.CO2NET/blob/master/Sample/Senparc.CO2NET.Sample.netcore3/Startup.cs
            var registerService = app
                //Global registration
                .UseSenparcGlobal(env, senparcSetting.Value, globalRegister =>
                {
                    //Configure global use of Redis cache (on-demand, independent)
                    SetCache(senparcSetting);

                    #region 注册日志（按需，建议）

                    globalRegister.RegisterTraceLog(ConfigTraceLog); //Configure TraceLog

                    #endregion
                });

            //XncfModules (required)
            app.UseXncfModules(registerService)
               .UseNcfDatabase<TDatabaseConfiguration>();

            /*  UseNcfDatabase<TDatabaseConfiguration>() generic type description
             *                
             * Method | Description
             * -------------------------------------------------|-------------------------
             * UseNcf<BySettingDatabaseConfiguration>() | The configuration is determined by appsettings.json
             * UseNcf<SqlServerDatabaseConfiguration>() | Use SQLServer database
             * UseNcf<SqliteMemoryDatabaseConfiguration>() | Use SQLite database
             * UseNcf<MySqlDatabaseConfiguration>() | Use MySQL database
             * UseNcf<PostgreSQLDatabaseConfiguration>() | Use PostgreSQL database
             * UseNcf<OracleDatabaseConfiguration>() | Use Oracle database (V12+)
             * UseNcf<OracleDatabaseConfigurationForV11>() | Use Oracle database (V11+)
             * UseNcf<DmDatabaseConfiguration>() | Use Dameng database
             * More databases can be expanded, and so on...
             *  
             */
        }

        /// <summary>
        /// Configure global use of Redis cache (on-demand, independent)
        /// </summary>
        /// <param name="senparcSetting"></param>
        private static void SetCache(IOptions<SenparcSetting> senparcSetting)
        {
            if (UseRedis(senparcSetting.Value, out string redisConfigurationStr))//In order to facilitate the configuration of developers in different environments, a judgment method is made here. The actual development environment is generally determined, and the if condition here can be ignored.
            {
                /* illustrate:
                 * 1. Redis connection string information will be automatically obtained and registered from Config.SenparcSetting.Cache_Redis_Configuration. If no modification is required, the following method can be ignored.
                /* 2. If you need to modify it manually, you can manually set the Redis link information through the SetConfigurationOption method below (only modify the configuration, not enable it immediately)
                 */
                Senparc.CO2NET.Cache.CsRedis.Register.SetConfigurationOption(redisConfigurationStr);

                //The following will immediately set the global cache to Redis
                Senparc.CO2NET.Cache.CsRedis.Register.UseKeyValueRedisNow(); //Key-value pair caching strategy (recommended)
                /*Senparc.CO2NET.Cache.CsRedis.Register.UseHashRedisNow();*/ //Cache strategy for HashSet storage format 

                //You can also customize the caching strategy that currently needs to be enabled in the following ways
                //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisObjectCacheStrategy.Instance);//key value pair
                //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisHashSetObjectCacheStrategy.Instance);//HashSet
            }
            //If the Redis cache is not enabled here, the memory cache is still used by default. 
        }

        /// <summary>
        /// Output startup success flag
        /// </summary>
        /// <param name="app"></param>
        public static void ShowSuccessTip(this WebApplication app)
        {
            //Output startup success flag
            Senparc.Ncf.Core.VersionManager.ShowSuccessTip($"\t\t启动工作准备就绪\r\n\t\t用时：{SystemTime.NowDiff(StartTime).TotalSeconds} s");
        }

        /// <summary>
        /// Determine whether the current configuration is sufficient to use Redis (determined based on whether the default configuration string has been modified)
        /// </summary>
        /// <param name="senparcSetting"></param>
        /// <returns></returns>
        internal static bool UseRedis(SenparcSetting senparcSetting, out string redisConfigurationStr)
        {
            redisConfigurationStr = senparcSetting.Cache_Redis_Configuration;
            var useRedis = !string.IsNullOrEmpty(redisConfigurationStr) && redisConfigurationStr != "#{Cache_Redis_Configuration}#"/*Default value, not enabled*/;
            return useRedis;
        }

        /// <summary>
        /// Configure WeChat tracking log
        /// </summary>
        private static void ConfigTraceLog()
        {
            //When this is set to Debug state, a log file will be generated in the /App_Data/WeixinTraceLog/ directory to record all API request logs. It is recommended to close it for the official release version.

            //If the global IsDebug (Senparc.CO2NET.Config.IsDebug) is false, you can set true here separately, otherwise it will automatically be true
            Senparc.CO2NET.Trace.SenparcTrace.SendCustomLog("系统日志",
                "NeuCharFramework 系统启动"); //Only takes effect when Senparc.Weixin.Config.IsDebug = true

            //Global custom logging callback
            Senparc.CO2NET.Trace.SenparcTrace.OnLogFunc = () =>
            {
                //Add the code that needs to be executed every time Log is triggered
            };
        }

    }
}
