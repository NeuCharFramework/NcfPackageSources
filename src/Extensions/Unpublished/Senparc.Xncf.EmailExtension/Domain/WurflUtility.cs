/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WurflUtility.cs
    文件功能描述：WurflUtility 相关实现
    
    
    创建标识：Senparc - 20211124
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
