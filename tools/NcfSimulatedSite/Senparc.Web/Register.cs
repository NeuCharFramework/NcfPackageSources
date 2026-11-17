/*----------------------------------------------------------------



    Copyright (C) 2026 Senparc



  



    File name: Register.cs



    File description: module registration and initialization logic











    Created by: Senparc - 20241028







    Modified by: Senparc - 20260702



    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling







    Modified by: Senparc - 20260804



    Change description: v0.35.0 Updated module behavior and related support.







    Modified by: Senparc - 20260812



    Change description: Updated module behavior and related support.







    Modified by: Senparc - 20260813



    Change description: v0.36.0 Updated module behavior and related support.







    Modified by: Senparc - 20260822



    Change description: v0.37.0 Updated module behavior and related support.







    Modified by: Senparc - 20261110



    Change description: Updated module behavior and related support.







    Modified by: Senparc - 20261111



    Change description: v0.39.8 Updated module behavior and related support.







----------------------------------------------------------------*/



using Microsoft.AspNetCore.DataProtection;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Options;

using Senparc.AI.Interfaces;

using Senparc.AI.AgentKernel;

using Senparc.Areas.Admin.Domain.Services;

using Senparc.CO2NET;

using Senparc.CO2NET.AspNet;

using Senparc.Ncf.Core.Models;

using Senparc.Ncf.Core.Localization;

using Senparc.Ncf.XncfBase;

using Senparc.Xncf.AreasBase;

using Senparc.Ncf.Core.EventBus;

using Senparc.Web.Controllers;

using Senparc.Web.Infrastructure.Database;



namespace Senparc.Web

{

    /// <summary>

    /// global registration

    /// </summary>

    public static class Register

    {

        private static System.DateTime StartTime = SystemTime.Now.DateTime;



        public static void AddNcf(this WebApplicationBuilder builder)

        {

            StartTime = SystemTime.Now.DateTime;



            //Legacy comment translated to English.
            //Legacy comment translated to English.
            builder.Services.Configure<HostOptions>(options =>

            {

                options.ShutdownTimeout = TimeSpan.FromSeconds(2);

            });



            // Localization baseline for all modules (Web/Admin/Installer/XNCF).

            // Use type-based lookup without ResourcesPath override to match embedded resource names.

            builder.Services.AddNcfLocalization();

            builder.Services.AddSingleton<DatabaseRuntimeStateStore>();

            builder.Services.AddSingleton<DatabaseUpgradeCoordinator>();



            //Legacy comment translated to English.
            var logMsg = builder.StartWebEngine(new[] { "Senparc.Areas.Admin"});

            //Legacy comment translated to English.


            Console.WriteLine("============ logMsg =============");

            Console.WriteLine(logMsg);

            Console.WriteLine("============ logMsg END =============");



            #region Legacy section


            //Legacy comment translated to English.
            //Legacy comment translated to English.
            var dataProtectionKeysDirectory = Path.Combine(

                builder.Environment.ContentRootPath,

                "App_Data",

                "DataProtection-Keys");

            Directory.CreateDirectory(dataProtectionKeysDirectory);

            builder.Services.AddDataProtection()

                .SetApplicationName("Senparc.NCF")

                .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysDirectory));



            //Legacy comment translated to English.
            // Windows: %LOCALAPPDATA%\ASP.NET\DataProtection-Keys）。

            //Legacy comment translated to English.
            // builder.Services.AddDataProtection().SetApplicationName("Senparc.NCF");



            #endregion



            //Legacy comment translated to English.
            //Legacy comment translated to English.
            var assembliesToScan = AppDomain.CurrentDomain.GetAssemblies()

                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.FullName))

                .Where(a =>

                {

                    var name = a.GetName().Name ?? string.Empty;

                    return name.Contains("Senparc.Xncf.", StringComparison.Ordinal) ||

                           name.Contains("Senparc.Areas.", StringComparison.Ordinal) ||

                           name.Contains(".Xncf.", StringComparison.Ordinal);

                })

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



            #region Legacy section


            //Legacy comment translated to English.
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



            //Legacy comment translated to English.
            //https://docs.microsoft.com/zh-cn/aspnet/core/host-and-deploy/iis/index?view=aspnetcore-2.1&tabs=aspnetcore2x#supported-operating-systems

            //services.Configure<IISOptions>(options =>

            //{

            //    options.ForwardClientCertificate = false;

            //});



            //Legacy comment translated to English.
            //services.AddHttpsRedirection(options =>

            //{

            //    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;

            //    options.HttpsPort = 443;

            //});

        }



        public static void UseNcf<TDatabaseConfiguration>(this WebApplication app, bool startBackgroundThreads = true)

            where TDatabaseConfiguration : IDatabaseConfiguration, new()

        {

            //Legacy comment translated to English.
            app.UseSenparcMvcDI();



            IWebHostEnvironment env = app.Environment;

            IOptions<SenparcSetting> senparcSetting = app.Services.GetService<IOptions<SenparcSetting>>();

            IOptions<SenparcCoreSetting> senparcCoreSetting = app.Services.GetService<IOptions<SenparcCoreSetting>>();



            // start CO2NET global registration，must！

            //Legacy comment translated to English.
            var registerService = app

                //global registration

                .UseSenparcGlobal(env, senparcSetting.Value, globalRegister =>

                {

                    //Legacy comment translated to English.
                    SetCache(senparcSetting);



                    #region Legacy section


                    globalRegister.RegisterTraceLog(ConfigTraceLog); //configurationTraceLog



                    #endregion

                });



            //XncfModules（must）

            app.UseXncfModules(registerService, startBackgroundThreads: startBackgroundThreads)

               .UseNcfDatabase<TDatabaseConfiguration>();



            /*  Legacy comment translated to English.

             *                

             *                  method                            |         description

             * -------------------------------------------------|-------------------------

             *  Legacy comment translated to English.

             *  UseNcf<SqlServerDatabaseConfiguration>()        |  use SQLServer database

             *  UseNcf<SqliteMemoryDatabaseConfiguration>()     |  use SQLite database

             *  UseNcf<MySqlDatabaseConfiguration>()            |  use MySQL database

             *  UseNcf<PostgreSQLDatabaseConfiguration>()       |  use PostgreSQL database

             *  UseNcf<OracleDatabaseConfiguration>()           |  use Oracle database（V12+）

             *  UseNcf<OracleDatabaseConfigurationForV11>()     |  use Oracle database（V11+）

             *  Legacy comment translated to English.

             *  Legacy comment translated to English.

             *  

             */

        }



        /// <summary>

        ///Legacy comment translated to English.
        /// </summary>

        /// <param name="senparcSetting"></param>

        private static void SetCache(IOptions<SenparcSetting> senparcSetting)

        {

            if (UseRedis(senparcSetting.Value, out string redisConfigurationStr))//Legacy comment translated to English.
            {

                /* description：

                 * Legacy comment translated to English.

                Legacy comment translated to English.

                 */

                Senparc.CO2NET.Cache.CsRedis.Register.SetConfigurationOption(redisConfigurationStr);



                //Legacy comment translated to English.
                Senparc.CO2NET.Cache.CsRedis.Register.UseKeyValueRedisNow(); //Legacy comment translated to English.
                /*Senparc.CO2NET.Cache.CsRedis.Register.UseHashRedisNow();*/ //Legacy comment translated to English.


                //Legacy comment translated to English.
                //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisObjectCacheStrategy.Instance);//key-value pair

                //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisHashSetObjectCacheStrategy.Instance);//HashSet

            }

            //Legacy comment translated to English.
        }



        /// <summary>

        /// output the startup success marker

        /// </summary>

        /// <param name="app"></param>

        public static void ShowSuccessTip(this WebApplication app)

        {

            //output the startup success marker

            Senparc.Ncf.Core.VersionManager.ShowSuccessTip($"\t\tStartup is ready\r\n\t\tStarted at: {StartTime:yyyy-MM-dd HH:mm:ss}\r\n\t\tElapsed: {SystemTime.NowDiff(StartTime).TotalSeconds} s");

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

        ///Legacy comment translated to English.
        /// </summary>

        private static void ConfigTraceLog()

        {

            //Legacy comment translated to English.


            //Legacy comment translated to English.
            Senparc.CO2NET.Trace.SenparcTrace.SendCustomLog("系统日志",

                "NeuCharFramework 系统启动"); //Legacy comment translated to English.


            //Legacy comment translated to English.
            Senparc.CO2NET.Trace.SenparcTrace.OnLogFunc = () =>

            {

                //Legacy comment translated to English.
            };

        }



    }

}

