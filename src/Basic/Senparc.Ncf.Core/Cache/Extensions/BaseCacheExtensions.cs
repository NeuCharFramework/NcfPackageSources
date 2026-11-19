/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BaseCacheExtensions.cs
    File description: BaseCacheExtensions related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Cache.Extensions
{
    public static class BaseCacheExtensions
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="baseCache"></param>
        /// <returns></returns>
        public static IDictionary<string, object> GetAll<T>(this IBaseCache<T> baseCache) where T : class, new()
        {
            return baseCache.Cache.GetAll();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="baseCache"></param>
        /// <returns></returns>
        public static Task<IDictionary<string, object>> GetAsync<T>(this IBaseCache<T> baseCache) where T : class, new()
        {
            return baseCache.Cache.GetAllAsync();
        }

        /// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
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
