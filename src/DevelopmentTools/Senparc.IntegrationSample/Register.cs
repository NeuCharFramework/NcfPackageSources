/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20230426

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.0.19 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.0.20 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.Extensions.Options;

using Senparc.CO2NET;
using Senparc.CO2NET.AspNet;
using Senparc.CO2NET.Utilities;
using Senparc.CO2NET.WebApi.WebApiEngines;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using Senparc.Weixin.WxOpen.AdvancedAPIs.WxApp.WxAppJson;
using Senparc.Xncf.AreasBase;

using Senparc.Ncf.Utility.Helpers;

namespace Senparc.IntegrationSample
{
    /// <summary>
    /// Provides the integration sample's application and database registration helpers.
    /// </summary>
    public static class Register
    {
        /// <summary>
        /// Gets the timestamp captured when the integration sample registration type
        /// was first initialized.
        /// </summary>
        public static DateTime StartTime { get; }

        /// <summary>
        /// Captures the process-local startup timestamp used by the success banner.
        /// </summary>
        static Register()
        {
            StartTime = System.DateTime.Now;
        }

        /// <summary>
        /// Registers the integration sample's global web-engine modules and optional
        /// diagnostics on the application builder.
        /// </summary>
        /// <param name="builder">The web application builder receiving configuration.</param>
        public static void AddNcf(this WebApplicationBuilder builder)
        {
            var logMsg = builder.StartWebEngine(new[] { "Senparc.IntegrationSample.AreaTests","Senparc.Weixin.MP" });
            Console.WriteLine("============ logMsg =============");
            Console.WriteLine(logMsg);
            Console.WriteLine("============ logMsg END =============");

            //var mvcCoreBuilder = builder.Services.AddMvcCore();
            //builder.Services.AddAndInitDynamicApi(mvcCoreBuilder, options => options.DocXmlPath = ServerUtility.ContentRootMapPath("~/App_Data/ApiDocXml"));
        }

        /// <summary>
        /// Applies global Senparc services, optional Redis configuration, XNCF modules,
        /// and the selected database configuration to the application pipeline.
        /// </summary>
        /// <typeparam name="TDatabaseConfiguration">The parameterless database configuration type.</typeparam>
        /// <param name="app">The web application receiving the registrations.</param>
        public static void UseNcf<TDatabaseConfiguration>(this WebApplication app)
            where TDatabaseConfiguration : IDatabaseConfiguration, new()
        {
            var env = app.Environment;

            // CO2NET global registration must run before module and database setup.
            var registerService = app
                //global registration
                .UseSenparcGlobal(env, null, globalRegister =>
                {
                    if (UseRedis(null, out var redisConfigurationStr))//Legacy comment translated to English.
                    {
                        /* description：

                         * Legacy comment translated to English.

                        Legacy comment translated to English.

                         */
                        CO2NET.Cache.CsRedis.Register.SetConfigurationOption(redisConfigurationStr);

                        CO2NET.Cache.CsRedis.Register.UseKeyValueRedisNow(); //Legacy comment translated to English.
                        /*Senparc.CO2NET.Cache.CsRedis.Register.UseHashRedisNow();*/ //Legacy comment translated to English.

                        //Legacy comment translated to English.
                        //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisObjectCacheStrategy.Instance);//key-value pair
                        //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisHashSetObjectCacheStrategy.Instance);//HashSet
                    }
                });

            //XncfModules（must）
            app.UseXncfModules(registerService);

            //mustin UseXncfModules after
            app.UseNcfDatabase(typeof(TDatabaseConfiguration));

        }

        /// <summary>
        /// Determines whether Redis is enabled by the configured cache connection string.
        /// </summary>
        /// <param name="senparcSetting">
        /// Optional settings to inspect. When omitted, the global Senparc setting is used.
        /// </param>
        /// <param name="redisConfigurationStr">
        /// Receives the configured Redis connection string, including placeholder values.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a non-empty, non-placeholder Redis configuration exists.
        /// </returns>
        internal static bool UseRedis(SenparcSetting? senparcSetting, out string redisConfigurationStr)
        {
            senparcSetting ??= Senparc.CO2NET.Config.SenparcSetting;
            redisConfigurationStr = senparcSetting.Cache_Redis_Configuration;
            var useRedis = !string.IsNullOrEmpty(redisConfigurationStr) && redisConfigurationStr != "#{Cache_Redis_Configuration}#"/*default value; disabled*/;
            return useRedis;
        }

        /// <summary>
        /// Writes the localized startup success marker, version information, and elapsed time.
        /// </summary>
        /// <param name="app">The started web application.</param>
        public static void ShowSuccessTip(this WebApplication app)
        {
            //output the startup success marker
            var ready = "";
            var startedAt = "";
            var elapsed = "";

            GlobalCulture.Create()
                .SetEnglish(() =>
                {
                    ready = "Startup is ready";
                    startedAt = "Started at: ";
                    elapsed = "Elapsed: ";
                })
                .SetChinese(() =>
                {
                    ready = "启动工作准备就绪";
                    startedAt = "启动时间：";
                    elapsed = "用时：";
                })
                .InvokeDefault();

            Senparc.Ncf.Core.VersionManager.ShowSuccessTip($"\t\t{ready}\r\n\t\t{startedAt}{StartTime:yyyy-MM-dd HH:mm:ss}\r\n\t\t{elapsed}{SystemTime.NowDiff(StartTime).TotalSeconds} s");
        }

    }
}
