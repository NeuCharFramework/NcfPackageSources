/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MethodCache.cs
    File description: MethodCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Threading.Tasks;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;

namespace Senparc.Ncf.Core.Cache
{
    public static class MethodCache
    {
        #region Legacy section

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        ///Legacy comment translated to English.
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        public static T GetMethodCache<T>(string cacheKey, Func<T> func, int timeoutSeconds) where T : class
        {

            cacheKey = cacheKey.ToUpper();

            var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();

            T result;

            if (!cache.CheckExisted(cacheKey))
            {
                cache.Set(cacheKey, func(), //Legacy comment translated to English.
                   TimeSpan.FromSeconds(timeoutSeconds));
            }

            result = cache.Get<T>(cacheKey);//outputresult

            return result;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static T GetMethodCache<T>(string cacheKey, Func<T> func) where T : class
        {
            return GetMethodCache(cacheKey, func, 60 * 60);
        }

        /// <summary>
        /// clear the cache
        /// </summary>
        /// <param name="cacheKey"></param>
        public static void ClearMethodCache<T>(string cacheKey) where T : class //Legacy comment translated to English.
        {
            cacheKey = cacheKey.ToUpper();

            var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();

            cache.RemoveFromCache(cacheKey);

            Console.WriteLine("Cache:" + cache.Get(cacheKey)?.ToJson(true));
        }
        #endregion


        #region Legacy section

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        ///Legacy comment translated to English.
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        public static async Task<T> GetMethodCacheAsync<T>(string cacheKey, Func<Task<T>> func, int timeoutSeconds) where T : class
        {
            cacheKey = cacheKey.ToUpper();
            var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();
            T result;

            if (!await cache.CheckExistedAsync(cacheKey))
            {
                var value = await func();
                await cache.SetAsync(cacheKey, value, TimeSpan.FromSeconds(timeoutSeconds));
            }

            result = await cache.GetAsync<T>(cacheKey);
            return result;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static Task<T> GetMethodCacheAsync<T>(string cacheKey, Func<Task<T>> func) where T : class
        {
            return GetMethodCacheAsync(cacheKey, func, 60 * 60);
        }

        /// <summary>  
        /// clear the cache  
        /// </summary>  
        /// <param name="cacheKey"></param>  
        public static async Task ClearMethodCacheAsync<T>(string cacheKey) where T : class
        {
            cacheKey = cacheKey.ToUpper();
            var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();
            await cache.RemoveFromCacheAsync(cacheKey);
            var cacheResult = await cache.GetAsync<T>(cacheKey);
            Console.WriteLine("Cache:" + (cacheResult != null ? cacheResult.ToJson(true) : "null"));
        }

        #endregion


    }
}
