using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Cache.Extensions
{
    public static class BaseCacheExtensions
    {
        /// <summary>
        /// Get all cache collections
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="baseCache"></param>
        /// <returns></returns>
        public static IDictionary<string, object> GetAll<T>(this IBaseCache<T> baseCache) where T : class, new()
        {
            return baseCache.Cache.GetAll();
        }

        /// <summary>
        /// [Async] Get all cache information collections
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="baseCache"></param>
        /// <returns></returns>
        public static Task<IDictionary<string, object>> GetAsync<T>(this IBaseCache<T> baseCache) where T : class, new()
        {
            return baseCache.Cache.GetAllAsync();
        }

        /// <summary>
        /// Get all cache information collections prefixed by key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="baseCache"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static IList<T> GetAllByPrefix<T>(this IBaseCache<T> baseCache, string key) where T : class, new()
        {
            return baseCache.Cache.GetAllByPrefix<T>(key);
        }

        /// <summary>
        /// [Async] Get all cache information collections prefixed by key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="baseCache"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Task<IList<T>> GetAllByPrefixAsync<T>(this IBaseCache<T> baseCache, string key) where T : class, new()
        {
            return baseCache.Cache.GetAllByPrefixAsync<T>(key);
        }
    }
}
