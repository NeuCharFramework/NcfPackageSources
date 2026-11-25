/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BaseCache.cs
    File description: BaseCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.CO2NET.Cache;
using Senparc.Ncf.Core.DI;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Utility;
using System;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Cache
{
    /// <summary>
    /// Provides the common state and cache access workflow used by cache-backed domain objects.
    /// </summary>
    /// <typeparam name="T">The reference type stored in the cache.</typeparam>
    public abstract class BaseCache<T> : IBaseCache<T> where T : class, new()
    {
        /// <summary>
        /// Writes an object to the configured cache using the supplied cache key.
        /// </summary>
        /// <param name="key">The complete cache key under which <paramref name="obj"/> is stored.</param>
        /// <param name="obj">The object to store.</param>
        /// <returns><see langword="true"/> after the cache write has been requested successfully.</returns>
        protected virtual bool UpdateToCache(string key, T obj)
        {
            Cache.Set(key, obj);
            return true;
        }

        /// <summary>
        /// Asynchronously writes an object to the configured cache using the supplied cache key.
        /// </summary>
        /// <param name="key">The complete cache key under which <paramref name="obj"/> is stored.</param>
        /// <param name="obj">The object to store.</param>
        /// <returns>A task whose result is <see langword="true"/> after the cache write completes.</returns>
        protected virtual async Task<bool> UpdateToCacheAsync(string key, T obj)
        {
            await Cache.SetAsync(key, obj).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Initializes a cache instance without assigning a cache key or database dependency.
        /// </summary>
        public BaseCache() { }

        /// <summary>
        /// Represents a callback that can persist or otherwise process a value after it is placed in the cache.
        /// </summary>
        /// <param name="obj">The value that was written to the cache.</param>
        public delegate void UpdateWithBataBase(T obj);

        protected INcfDbData _db;

        protected string CacheKey;
        private T _data;

        public string CacheSetKey { get; private set; }

        public DateTime CacheTime { get; set; }
        public DateTime CacheTimeOut { get; set; }

        //private ICacheStrategy _cache;

        /// <summary>
        /// Gets or sets the cache strategy used for all cache reads and writes performed by this instance.
        /// </summary>
        public IBaseObjectCacheStrategy Cache { get; set; }

        /// <summary>
        /// Gets or sets the default cache lifetime, in minutes, used when a value is written through
        /// <see cref="Data"/> without an explicit timeout.
        /// </summary>
        public int TimeOut { get; set; }

        /// <summary>
        /// Initializes a cache instance with a logical cache key and the default cache strategy.
        /// </summary>
        /// <param name="cacheKey">The logical key used to identify the cached value.</param>
        public BaseCache(string cacheKey)
            : this(cacheKey, null)
        { }

        /// <summary>
        /// Initializes a cache instance with a logical cache key and an optional database dependency.
        /// </summary>
        /// <param name="cacheKey">The logical key used to identify the cached value.</param>
        /// <param name="db">An optional database abstraction used by derived classes when refreshing or persisting data.</param>
        public BaseCache(string cacheKey, INcfDbData db)
        {
            CacheKey = cacheKey;

            _db = db;
            if (TimeOut == 0)
            {
                TimeOut = 1440;
            }

            Cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();
            this.CacheSetKey = cacheKey;//Legacy comment translated to English.
        }

        #region Legacy section

        /// <summary>
        /// Gets the cached value, loading it through <see cref="Update"/> when the configured cache
        /// does not contain the logical key, or writes a new value using the configured timeout.
        /// </summary>
        /// <exception cref="Exception">
        /// Thrown when the cache strategy has not been initialized before a read is attempted.
        /// </exception>
        public virtual T Data
        {
            get
            {
                if (_data != null)
                {
                    return _data;
                }

                if (Cache == null)
                {
                    var msg = "Cache==null! 系统调试记录cache一个bug(101)。";
                    throw new Exception(msg);
                }

                if (Cache.Get(CacheKey) == null)
                {
                    _data = Update();
                }
                return Cache.Get<T>(CacheKey);
            }
            set => Cache.Set(CacheKey, value, TimeSpan.FromMinutes(TimeOut));
        }

        /// <summary>
        /// Stores a value with an explicit lifetime and optionally invokes a callback that can
        /// synchronize the same value with a database or another persistence boundary.
        /// </summary>
        /// <param name="value">The value to place in the cache.</param>
        /// <param name="timeOut">The cache lifetime in minutes.</param>
        /// <param name="updateWithDatabases">
        /// An optional callback invoked after the cache write. The callback receives <paramref name="value"/>.
        /// </param>
        public virtual void SetData(T value, int timeOut, UpdateWithBataBase updateWithDatabases)
        {
            Cache.Set(CacheKey, value, TimeSpan.FromMinutes(timeOut));

            this.CacheTime = DateTime.Now;
            this.CacheTimeOut = this.CacheTime.AddMinutes(timeOut);

            updateWithDatabases?.Invoke(value);
        }

        /// <summary>
        /// Removes the value associated with this instance's logical cache key.
        /// </summary>
        public virtual void RemoveCache()
        {
            Cache.RemoveFromCache(CacheKey);
        }

        /// <summary>
        /// Provides the synchronous refresh hook used by <see cref="Data"/> when the cache is empty.
        /// The base implementation does not load a value and returns <see langword="null"/>.
        /// </summary>
        /// <returns>The value loaded by a derived cache implementation, or <see langword="null"/> in the base implementation.</returns>
        public virtual T Update()
        {
            return null;
        }

        /// <summary>
        /// Provides the synchronous persistence hook for a refreshed or explicitly supplied value.
        /// The base implementation intentionally performs no database operation.
        /// </summary>
        /// <param name="obj">The value that a derived implementation may persist.</param>
        public virtual void UpdateToDatabase(T obj)
        {
        }

        #endregion

        #region Legacy section
        /// <summary>
        /// Asynchronously gets the cached value, loading it through <see cref="UpdateAsync"/> when
        /// the configured cache does not contain the logical key.
        /// </summary>
        /// <returns>A task whose result is the cached or refreshed value.</returns>
        /// <exception cref="Exception">
        /// Thrown when the cache strategy has not been initialized before an asynchronous read is attempted.
        /// </exception>
        public virtual async Task<T> GetDataAsync()
        {
            if (_data != null)
            {
                return _data;
            }

            if (Cache == null)
            {
                var msg = "Cache==null! 系统调试记录cache一个bug(101)。";
                throw new Exception(msg);
            }

            if (await Cache.GetAsync(CacheKey).ConfigureAwait(false) == null)
            {
                _data = await UpdateAsync();
            }
            return await Cache.GetAsync<T>(CacheKey).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously stores a value with an explicit lifetime and optionally invokes a callback
        /// that can synchronize the value with a database or another persistence boundary.
        /// </summary>
        /// <param name="value">The value to place in the cache.</param>
        /// <param name="timeOut">The cache lifetime in minutes.</param>
        /// <param name="updateWithDatabases">
        /// An optional callback invoked after the cache write. The callback receives <paramref name="value"/>.
        /// </param>
        public virtual async Task SetDataAsync(T value, int timeOut, UpdateWithBataBase updateWithDatabases)
        {
            await Cache.SetAsync(CacheKey, value, TimeSpan.FromMinutes(timeOut)).ConfigureAwait(false);

            this.CacheTime = DateTime.Now;
            this.CacheTimeOut = this.CacheTime.AddMinutes(timeOut);

            updateWithDatabases?.Invoke(value);
        }

        /// <summary>
        /// Asynchronously removes the value associated with this instance's logical cache key.
        /// </summary>
        /// <returns>A task that completes after the cache entry has been removed.</returns>
        public virtual async Task RemoveCacheAsync()
        {
            await Cache.RemoveFromCacheAsync(CacheKey).ConfigureAwait(false);
        }

        /// <summary>
        /// Provides the asynchronous refresh hook used by <see cref="GetDataAsync"/>.
        /// The base implementation does not load a value and returns a completed task with
        /// <see langword="null"/>.
        /// </summary>
        /// <returns>A task whose result is the value loaded by a derived implementation.</returns>
        public virtual Task<T> UpdateAsync()
        {
            return null;
        }

        /// <summary>
        /// Provides the asynchronous persistence hook for a value.
        /// The base implementation intentionally performs no database operation.
        /// </summary>
        /// <param name="obj">The value that a derived implementation may persist.</param>
        /// <returns>A completed task in the base implementation.</returns>
        public virtual Task UpdateToDatabaseAsync(T obj)
        {
            return Task.CompletedTask;
        }

        #endregion
    }
}
