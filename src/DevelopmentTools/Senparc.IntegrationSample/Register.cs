/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20230426

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
    /// global registration
    /// </summary>
    public static class Register
    {
        public static DateTime StartTime { get; }

        static Register()
        {
            StartTime = System.DateTime.Now;
        }

        public static void AddNcf(this WebApplicationBuilder builder)
        {
            //Legacy comment translated to English.
            var logMsg = builder.StartWebEngine(new[] { "Senparc.IntegrationSample.AreaTests","Senparc.Weixin.MP" });
            Console.WriteLine("============ logMsg =============");
            Console.WriteLine(logMsg);
            Console.WriteLine("============ logMsg END =============");

            //var mvcCoreBuilder = builder.Services.AddMvcCore();
            //builder.Services.AddAndInitDynamicApi(mvcCoreBuilder, options => options.DocXmlPath = ServerUtility.ContentRootMapPath("~/App_Data/ApiDocXml"));
        }

        public static void UseNcf<TDatabaseConfiguration>(this WebApplication app)
            where TDatabaseConfiguration : IDatabaseConfiguration, new()
        {
            var env = app.Environment;

            // start CO2NET global registration，must！
            //Legacy comment translated to English.
            var registerService = app
                //global registration
                .UseSenparcGlobal(env, null, globalRegister =>
                {
                    //Legacy comment translated to English.
                    if (UseRedis(null, out var redisConfigurationStr))//Legacy comment translated to English.
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
            app.UseXncfModules(registerService);

            //mustin UseXncfModules after
            app.UseNcfDatabase(typeof(TDatabaseConfiguration));

        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="senparcSetting"></param>
        /// <returns></returns>
        internal static bool UseRedis(SenparcSetting? senparcSetting, out string redisConfigurationStr)
        {
            senparcSetting ??= Senparc.CO2NET.Config.SenparcSetting;
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
