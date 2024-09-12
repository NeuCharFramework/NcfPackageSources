using System;
using System.Threading.Tasks;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;

namespace Senparc.Ncf.Core.Cache
{
    public static class MethodCache
    {
        #region 同步方法

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="func">当未命中缓存时存入并返回的结果</param>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        public static T GetMethodCache<T>(string cacheKey, Func<T> func, int timeoutSeconds) where T : class
        {

            cacheKey = cacheKey.ToUpper();

            var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();

            T result;

            if (!cache.CheckExisted(cacheKey))
            {
                cache.Set(cacheKey, func(), //每次储存的是重新执行过的最新的结果
                   TimeSpan.FromSeconds(timeoutSeconds));
            }

            result = cache.Get<T>(cacheKey);//输出结果

            return result;
        }

        /// <summary>
        /// 获取缓存，默认缓存时间为 60 分钟
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="func">当未命中缓存时存入并返回的结果</param>
        /// <returns></returns>
        public static T GetMethodCache<T>(string cacheKey, Func<T> func) where T : class
        {
            return GetMethodCache(cacheKey, func, 60 * 60);
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        /// <param name="cacheKey"></param>
        public static void ClearMethodCache<T>(string cacheKey) where T : class //虽然这边的T不需要传入，不过为了拿到CacheStrategy仍然需要提供
        {
            cacheKey = cacheKey.ToUpper();

            var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();

            cache.RemoveFromCache(cacheKey);

            Console.WriteLine("Cache:" + cache.Get(cacheKey)?.ToJson(true));
        }
        #endregion


        #region 异步方法  

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="func">当未命中缓存时存入并返回的结果</param>
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
        /// 获取缓存，默认缓存时间为 60 分钟
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="func">当未命中缓存时存入并返回的结果</param>
        /// <returns></returns>
        public static Task<T> GetMethodCacheAsync<T>(string cacheKey, Func<Task<T>> func) where T : class
        {
            return GetMethodCacheAsync(cacheKey, func, 60 * 60);
        }

        /// <summary>  
        /// 清除缓存  
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
