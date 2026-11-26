/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IBaseCacheStrategy.cs
    File description: IBaseCacheStrategy related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

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
    /// Extends the shared cache strategy with the cache-set identifier used by NCF.
    /// </summary>
    public interface INcfCacheStrategy : IBaseObjectCacheStrategy
    { 
        /// <summary>
        /// Gets or sets the logical cache-set key.
        /// </summary>
        string CacheSetKey { get; set; }
    }
}
