/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BaseCacheExtensions.cs
    File description: BaseCacheExtensions related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

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
        /// Gets all raw cache entries from the underlying cache strategy.
        /// </summary>
        /// <typeparam name="T">The cached value type.</typeparam>
        /// <param name="baseCache">The cache wrapper.</param>
        /// <returns>All raw cache entries.</returns>
        public static IDictionary<string, object> GetAll<T>(this IBaseCache<T> baseCache) where T : class, new()
        {
            return baseCache.Cache.GetAll();
        }

        /// <summary>
        /// Asynchronously gets all raw cache entries.
        /// </summary>
        /// <typeparam name="T">The cached value type.</typeparam>
        /// <param name="baseCache">The cache wrapper.</param>
        /// <returns>A task containing all raw cache entries.</returns>
        public static Task<IDictionary<string, object>> GetAsync<T>(this IBaseCache<T> baseCache) where T : class, new()
        {
            return baseCache.Cache.GetAllAsync();
        }

        /// <summary>
        /// Gets all cached values whose keys share the supplied prefix.
        /// </summary>
        /// <typeparam name="T">The cached value type.</typeparam>
        /// <param name="baseCache">The cache wrapper.</param>
        /// <param name="key">The key prefix to match.</param>
        /// <returns>The matching cached values.</returns>
        public static IList<T> GetAllByPrefix<T>(this IBaseCache<T> baseCache, string key) where T : class, new()
        {
            return baseCache.Cache.GetAllByPrefix<T>(key);
        }

        /// <summary>
        /// Asynchronously gets all cached values whose keys share the supplied prefix.
        /// </summary>
        /// <typeparam name="T">The cached value type.</typeparam>
        /// <param name="baseCache">The cache wrapper.</param>
        /// <param name="key">The key prefix to match.</param>
        /// <returns>A task containing the matching cached values.</returns>
        public static Task<IList<T>> GetAllByPrefixAsync<T>(this IBaseCache<T> baseCache, string key) where T : class, new()
        {
            return baseCache.Cache.GetAllByPrefixAsync<T>(key);
        }
    }
}
