/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WurflUtility.cs
    File description: WurflUtility related implementation

    Created by: Senparc - 20211124

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.12.16 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Web;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.DependencyInjection;
//using Senparc.Core.Utility;
//using WURFL;
//using WURFL.Aspnet.Extensions.Config;

//namespace Senparc.Utility
//{
//    public static class WurflUtility
//    {
//        #region WURFL
//        public static object RegisterLock = new object();
//        public static string RegisterState = "unstart";
//        public static void RegisterWurfl(IMemoryCache cache)
//        {
//            var configurer = new ApplicationConfigurer();
//            var wurflManager = WURFLManagerBuilder.Build(configurer);
//            cache.Set("WurflManagerKey", wurflManager);
//        }

//        public static IWURFLManager WurflManager
//        {
//            get
//            {
//                lock (RegisterLock)
//                {

//                    var cache = DI.ServiceProvider.GetService<IMemoryCache>();

//                    var wurfl = cache.Get<IWURFLManager>("WurflManagerKey");
//                    if (wurfl == null)
//                    {
//                        RegisterWurfl(cache);
//                        wurfl = cache.Get<IWURFLManager>("WurflManagerKey");
//                    }
//                    return wurfl;
//                }
//            }
//        }

//        public static String WurflStartupTime;

//        #endregion
//    }
//}
