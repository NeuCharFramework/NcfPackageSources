/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: IBaseCacheStrategy.cs

    File description: IBaseCacheStrategy related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using Senparc.CO2NET.Cache;

namespace Senparc.Ncf.Core.Cache
{

    //public interface IBaseCacheStrategy
    //{
    //    /// <summary>
    //Legacy comment translated to English.
    //    /// </summary>
    //    /// <param name="resourceName"></param>
    //    /// <param name="key"></param>
    //    /// <param name="retryCount"></param>
    //    /// <param name="retryDelay"></param>
    //    /// <returns></returns>
    //    ICacheLock BeginCacheLock(string resourceName, string key, int retryCount = 0,
    //                TimeSpan retryDelay = new TimeSpan());

    //}

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public interface INcfCacheStrategy : IBaseObjectCacheStrategy
    { 
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        string CacheSetKey { get; set; }
    }
}
