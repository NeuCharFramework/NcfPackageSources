using Senparc.CO2NET;
using Senparc.CO2NET.Cache;
using Senparc.Ncf.Core.DI;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Utility;
using System;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Cache
{
    public abstract class BaseCache<T> : IBaseCache<T> where T : class, new()
    {
        protected virtual bool UpdateToCache(string key, T obj)
        {
            Cache.Set(key, obj);
            return true;
        }

        protected virtual async Task<bool> UpdateToCacheAsync(string key, T obj)
        {
            await Cache.SetAsync(key, obj).ConfigureAwait(false);
            return true;
        }

        public BaseCache() { }

        public delegate void UpdateWithBataBase(T obj);

        protected INcfDbData _db;

        protected string CacheKey;
        private T _data;

        public string CacheSetKey { get; private set; }

        public DateTime CacheTime { get; set; }
        public DateTime CacheTimeOut { get; set; }

        //private ICacheStrategy _cache;

        /// <summary>
        ///caching policy.
        /// Please try not to call the method of this object outside BaseCache, especially the Key of Cache will be redefined in DictionaryCache
        /// </summary>
        public IBaseObjectCacheStrategy Cache { get; set; }
        /// <summary>
        /// Timeout time, 1400 minutes is 1 day.
        /// </summary>
        public int TimeOut { get; set; }

        public BaseCache(string cacheKey)
            : this(cacheKey, null)
        { }

        public BaseCache(string cacheKey, INcfDbData db)
        {
            CacheKey = cacheKey;

            _db = db;
            if (TimeOut == 0)
            {
                TimeOut = 1440;
            }

            Cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();
            this.CacheSetKey = cacheKey;//Set cache collection key, required
        }

        #region Synchronous Methods

        /// <summary>
        ///Data cannot be called in the Update() method, otherwise it will cause a loop call. SetData() method should be used in Update() method
        ///Data only works with simple types, not applicable if the cache type is a list
        /// </summary>
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
        ///Set the entire cache data
        /// </summary>
        /// <param name="value"></param>
        /// <param name="timeOut"></param>
        /// <param name="updateWithDatabases"></param>
        public virtual void SetData(T value, int timeOut, UpdateWithBataBase updateWithDatabases)
        {
            Cache.Set(CacheKey, value, TimeSpan.FromMinutes(timeOut));

            // Record cache time
            this.CacheTime = DateTime.Now;
            this.CacheTimeOut = this.CacheTime.AddMinutes(timeOut);

            updateWithDatabases?.Invoke(value);
        }

        public virtual void RemoveCache()
        {
            Cache.RemoveFromCache(CacheKey);
        }

        public virtual T Update()
        {
            return null;
        }

        public virtual void UpdateToDatabase(T obj)
        {
        }

        #endregion

        #region Asynchronous Methods
        /// <summary>
        /// Get all cached data
        ///Data cannot be called in the Update() method, otherwise it will cause a loop call. SetData() method should be used in Update() method
        ///Data only works with simple types, not applicable if the cache type is a list
        /// </summary>
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
        ///Set the entire cache data
        /// </summary>
        /// <param name="value"></param>
        /// <param name="timeOut"></param>
        /// <param name="updateWithDatabases"></param>
        public virtual async Task SetDataAsync(T value, int timeOut, UpdateWithBataBase updateWithDatabases)
        {
            await Cache.SetAsync(CacheKey, value, TimeSpan.FromMinutes(timeOut)).ConfigureAwait(false);

            // Record cache time
            this.CacheTime = DateTime.Now;
            this.CacheTimeOut = this.CacheTime.AddMinutes(timeOut);

            updateWithDatabases?.Invoke(value);
        }

        public virtual async Task RemoveCacheAsync()
        {
            await Cache.RemoveFromCacheAsync(CacheKey).ConfigureAwait(false);
        }

        public virtual Task<T> UpdateAsync()
        {
            return null;
        }

        public virtual Task UpdateToDatabaseAsync(T obj)
        {
            return Task.CompletedTask;
        }

        #endregion
    }
}
