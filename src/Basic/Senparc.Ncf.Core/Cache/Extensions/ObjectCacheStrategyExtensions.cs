/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ObjectCacheStrategyExtensions.cs
    File description: ObjectCacheStrategyExtensions related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Cache.CsRedis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Cache.Extensions
{
    public static class ObjectCacheStrategyExtensions
    {
        /// <summary>
        /// Retrieves all Redis cache entries whose keys share a prefix.
        /// </summary>
        /// <typeparam name="T">The cached value type.</typeparam>
        /// <param name="obj">The cache strategy.</param>
        /// <param name="key">The key prefix.</param>
        /// <returns>The matching cached values.</returns>
        /// <exception cref="Exception">Thrown when the strategy does not support prefix lookup.</exception>
        public static IList<T> GetAllByPrefix<T>(this IBaseObjectCacheStrategy obj, string key)
        {
            if (obj is RedisObjectCacheStrategy)
            {
                RedisObjectCacheStrategy _obj = obj as RedisObjectCacheStrategy;
                return _obj.GetAllByPrefix<T>(key);
            }
            throw new Exception("未实现或缓存不支持前缀方式获取缓存！");
        }

        /// <summary>
        /// Asynchronously retrieves all Redis cache entries whose keys share a prefix.
        /// </summary>
        /// <typeparam name="T">The cached value type.</typeparam>
        /// <param name="obj">The cache strategy.</param>
        /// <param name="key">The key prefix.</param>
        /// <returns>A task containing the matching cached values.</returns>
        /// <exception cref="Exception">Thrown when the strategy does not support prefix lookup.</exception>
        public static async Task<IList<T>> GetAllByPrefixAsync<T>(this IBaseObjectCacheStrategy obj, string key)
        {
            if (obj is RedisObjectCacheStrategy)
            {
                var _obj = obj as RedisObjectCacheStrategy;
                return await _obj.GetAllByPrefixAsync<T>(key);
            }
            throw new Exception("未实现或缓存不支持前缀方式获取缓存！");
        }
    }
}
