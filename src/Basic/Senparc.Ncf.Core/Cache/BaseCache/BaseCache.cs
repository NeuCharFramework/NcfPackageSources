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
        /// 缓存策略。
        /// 请尽量不要再BaseCache以外调用这个对象的方法，尤其Cache的Key在DictionaryCache中是会被重新定义的
        /// </summary>
        public IBaseObjectCacheStrategy Cache { get; set; }
        /// <summary>
        /// 超时时间，1400分钟为1天。
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
            this.CacheSetKey = cacheKey;//设置缓存集合键，必须提供
        }

        #region 同步方法

        /// <summary>
        /// Data不能在Update()方法中调用，否则会引发循环调用。Update()方法中应该使用SetData()方法
        /// Data只适用于简单类型，如果缓存类型为列表，则不适用
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
        /// 设置整个缓存数据
        /// </summary>
        /// <param name="value"></param>
        /// <param name="timeOut"></param>
        /// <param name="updateWithDatabases"></param>
        public virtual void SetData(T value, int timeOut, UpdateWithBataBase updateWithDatabases)
        {
            Cache.Set(CacheKey, value, TimeSpan.FromMinutes(timeOut));

            //记录缓存时间
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

        #region 异步方法
        /// <summary>
        /// 获取全部缓存数据
        /// Data不能在Update()方法中调用，否则会引发循环调用。Update()方法中应该使用SetData()方法
        /// Data只适用于简单类型，如果缓存类型为列表，则不适用
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
        /// 设置整个缓存数据
        /// </summary>
        /// <param name="value"></param>
        /// <param name="timeOut"></param>
        /// <param name="updateWithDatabases"></param>
        public virtual async Task SetDataAsync(T value, int timeOut, UpdateWithBataBase updateWithDatabases)
        {
            await Cache.SetAsync(CacheKey, value, TimeSpan.FromMinutes(timeOut)).ConfigureAwait(false);

            //记录缓存时间
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
