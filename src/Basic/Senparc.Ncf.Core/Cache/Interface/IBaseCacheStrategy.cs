/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IBaseCacheStrategy.cs
    文件功能描述：IBaseCacheStrategy 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using Senparc.CO2NET.Cache;

namespace Senparc.Ncf.Core.Cache
{

    //public interface IBaseCacheStrategy
    //{
    //    /// <summary>
    //    /// 开始一个同步锁
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
    /// 公共缓存策略接口
    /// </summary>
    public interface INcfCacheStrategy : IBaseObjectCacheStrategy
    { 
        /// <summary>
        /// 整个Cache集合的Key
        /// </summary>
        string CacheSetKey { get; set; }
    }
}
