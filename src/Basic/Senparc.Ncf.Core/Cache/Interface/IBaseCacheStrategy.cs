using System;
using System.Collections.Generic;
using Senparc.CO2NET.Cache;

namespace Senparc.Ncf.Core.Cache
{

    //public interface IBaseCacheStrategy
    //{
    //    /// <summary>
    //    /// Start a synchronization lock
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
    ///Public cache policy interface
    /// </summary>
    public interface INcfCacheStrategy : IBaseObjectCacheStrategy
    { 
        /// <summary>
        /// Key of the entire Cache collection
        /// </summary>
        string CacheSetKey { get; set; }
    }
}
