using System;
using System.Threading.Tasks;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;

namespace Senparc.Ncf.Core.Cache
{
    public static class MethodCache
    {
        #region Synchronous Methods

        /// <summary>
        /// Get cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="func">The result stored and returned when the cache is missed</param>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        public static T GetMethodCache<T>(string cacheKey, Func<T> func, int timeoutSeconds) where T : class
        {

            cacheKey = cacheKey.ToUpper();

            var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();

            T result;

            if (!cache.CheckExisted(cacheKey))
            {
                cache.Set(cacheKey, func(), //What is stored each time is the latest result of re-execution.
                   TimeSpan.FromSeconds(timeoutSeconds));
            }

            result = cache.Get<T>(cacheKey);//Output results

            return result;
        }

        /// <summary>
        /// Get cache, the default cache time is 60 minutes
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="func">The result stored and returned when the cache is missed</param>
        /// <returns></returns>
        public static T GetMethodCache<T>(string cacheKey, Func<T> func) where T : class
        {
            return GetMethodCache(cacheKey, func, 60 * 60);
        }

        /// <summary>
        /// clear cache
        /// </summary>
        /// <param name="cacheKey"></param>
        public static void ClearMethodCache<T>(string cacheKey) where T : class //Although T here does not need to be passed in, it still needs to be provided in order to get CacheStrategy
        {
            cacheKey = cacheKey.ToUpper();

            var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();

            cache.RemoveFromCache(cacheKey);

            Console.WriteLine("Cache:" + cache.Get(cacheKey)?.ToJson(true));
        }
        #endregion


        #region Asynchronous Methods  

        /// <summary>
        /// Get cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="func">The result stored and returned when the cache is missed</param>
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
        /// Get cache, the default cache time is 60 minutes
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="func">The result stored and returned when the cache is missed</param>
        /// <returns></returns>
        public static Task<T> GetMethodCacheAsync<T>(string cacheKey, Func<Task<T>> func) where T : class
        {
            return GetMethodCacheAsync(cacheKey, func, 60 * 60);
        }

        /// <summary>  
        /// clear cache  
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
