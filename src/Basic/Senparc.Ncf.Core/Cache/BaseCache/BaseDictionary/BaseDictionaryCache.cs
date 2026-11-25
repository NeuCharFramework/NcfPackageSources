/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BaseDictionaryCache.cs
    File description: BaseDictionaryCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Log;
using System.IO;
using Senparc.Ncf.Core.Utility;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Senparc.Ncf.Core.Cache
{
    /// <summary>
    /// Defines cache operations for values addressed by a separate dictionary key.
    /// </summary>
    /// <typeparam name="TKey">The type used to identify an individual cached value.</typeparam>
    /// <typeparam name="TValue">The value type stored in the cache.</typeparam>
    public interface IBaseDictionaryCache<TKey, TValue> : IBaseDictionaryCache<TKey, TValue, TValue>
        where TValue : class, new()
    {
    }

    /// <summary>
    /// Defines synchronous and asynchronous operations for a cache whose entries are addressed
    /// by a typed key and may be created from a separate entity type.
    /// </summary>
    /// <typeparam name="TKey">The type used to identify an individual cached value.</typeparam>
    /// <typeparam name="TValue">The value type stored in the cache.</typeparam>
    /// <typeparam name="TEntity">The source entity type used when constructing a cached value.</typeparam>
    public interface IBaseDictionaryCache<TKey, TValue, TEntity> : IBaseCache<TValue>
           where TValue : class, new()
    {
        /// <summary>
        /// Creates and caches a value for <paramref name="key"/> using the implementation-specific source.
        /// </summary>
        /// <param name="key">The typed key used to build the final cache key.</param>
        /// <returns>The created value, or <see langword="null"/> when the implementation cannot create one.</returns>
        TValue InsertObjectToCache(TKey key);

        /// <summary>
        /// Creates a cache value from <paramref name="obj"/>, stores it under <paramref name="key"/>,
        /// and returns the stored value.
        /// </summary>
        /// <param name="key">The typed key used to build the final cache key.</param>
        /// <param name="obj">The source entity used to populate the cached value.</param>
        /// <returns>The value stored in the cache, or <see langword="null"/> when <paramref name="obj"/> is <see langword="null"/>.</returns>
        TValue InsertObjectToCache(TKey key, TEntity obj);

        /// <summary>
        /// Gets the cached value for <paramref name="key"/>, creating it when no entry exists.
        /// </summary>
        /// <param name="key">The typed key used to locate the value.</param>
        /// <returns>The existing or newly created value, or <see langword="null"/> for a null key.</returns>
        TValue GetObject(TKey key);

        /// <summary>
        /// Removes the value associated with <paramref name="key"/> when that entry exists.
        /// </summary>
        /// <param name="key">The typed key used to locate the value.</param>
        void RemoveObject(TKey key);

        /// <summary>
        /// Stores <paramref name="obj"/> under a cache key derived from <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The typed key used to build the final cache key.</param>
        /// <param name="obj">The value to cache.</param>
        /// <returns><see langword="true"/> when the underlying cache write reports success.</returns>
        bool UpdateToCache(TKey key, TValue obj);

        /// <summary>
        /// Asynchronously creates and caches a value for <paramref name="key"/> using the implementation-specific source.
        /// </summary>
        /// <param name="key">The typed key used to build the final cache key.</param>
        /// <returns>A task whose result is the created value, or <see langword="null"/> when it cannot be created.</returns>
        Task<TValue> InsertObjectToCacheAsync(TKey key);

        /// <summary>
        /// Asynchronously creates a cache value from <paramref name="obj"/>, stores it under
        /// <paramref name="key"/>, and returns the stored value.
        /// </summary>
        /// <param name="key">The typed key used to build the final cache key.</param>
        /// <param name="obj">The source entity used to populate the cached value.</param>
        /// <returns>A task whose result is the stored value, or <see langword="null"/> for a null source entity.</returns>
        Task<TValue> InsertObjectToCacheAsync(TKey key, TEntity obj);

        /// <summary>
        /// Asynchronously gets the cached value for <paramref name="key"/>, creating it when no entry exists.
        /// </summary>
        /// <param name="key">The typed key used to locate the value.</param>
        /// <returns>A task whose result is the existing or newly created value, or <see langword="null"/> for a null key.</returns>
        Task<TValue> GetObjectAsync(TKey key);

        /// <summary>
        /// Asynchronously removes the value associated with <paramref name="key"/> when that entry exists.
        /// </summary>
        /// <param name="key">The typed key used to locate the value.</param>
        /// <returns>A task that completes after the cache entry has been removed.</returns>
        Task RemoveObjectAsync(TKey key);

        /// <summary>
        /// Asynchronously stores <paramref name="obj"/> under a cache key derived from <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The typed key used to build the final cache key.</param>
        /// <param name="obj">The value to cache.</param>
        /// <returns>A task whose result indicates whether the underlying cache write reports success.</returns>
        Task<bool> UpdateToCacheAsync(TKey key, TValue obj);
    }


    public abstract class BaseDictionaryCache<TKey, TValue> : BaseDictionaryCache<TKey, TValue, TValue>,
        IBaseDictionaryCache<TKey, TValue>
        where TValue : class, new()
    {

        /// <summary>
        /// Initializes a dictionary cache using the supplied logical cache key, database dependency,
        /// and entry lifetime.
        /// </summary>
        /// <param name="CACHE_KEY">The logical cache key used as the namespace for entries.</param>
        /// <param name="db">An optional database abstraction used by derived implementations.</param>
        /// <param name="timeOut">The default cache lifetime in minutes.</param>
        public BaseDictionaryCache(string CACHE_KEY, INcfDbData db, int timeOut)
            : base(CACHE_KEY, db, timeOut)
        {
            base.TimeOut = timeOut;
        }
    }

    public abstract class BaseDictionaryCache<TKey, TValue, TEntity> :
        BaseCache<TValue>, IBaseDictionaryCache<TKey, TValue, TEntity>
        where TValue : class, new()
        where TEntity : class/*, new()*/
    {
        /// <summary>
        /// Converts a typed dictionary key into the concrete cache key used by the configured
        /// cache strategy. Primitive values are represented directly; other values are serialized
        /// and represented by an MD5 digest.
        /// </summary>
        /// <param name="key">The key whose cache representation should be generated.</param>
        /// <returns>The concrete cache key used for cache operations.</returns>
        /// <exception cref="Exception">Thrown when <paramref name="key"/> is <see langword="null"/>.</exception>
        protected string GetFinalCacheKey(TKey key)
        {
            //TODO:checkKeytype
            TypeCode code = key == null ? TypeCode.DBNull : Type.GetTypeCode(key.GetType());
            string keyCode = null;
            switch (code)
            {
                case TypeCode.DBNull: throw new Exception("Key不允许为空！");
                case TypeCode.String:
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Char:
                case TypeCode.DateTime:
                case TypeCode.Double:
                case TypeCode.Single:
                    keyCode = key.ToString();
                    break;
                default:
                    code = TypeCode.Object;
                    string keyStr = System.Text.Json.JsonSerializer.Serialize(key);
                    keyCode = MD5.GetMD5Code(keyStr, "");
                    break;
            }

            string finalKey = null;

            if (base.Cache is Senparc.CO2NET.Cache.CsRedis.BaseRedisObjectCacheStrategy)
            {
                finalKey = $"{keyCode}";
            }
            else
            {
                finalKey = $"{CacheKey}@@@{keyCode}";
            }

            return finalKey;
        }

        //protected virtual ArraySegment<byte> SerializeObject(object value)
        //{
        //    using (var ms = new MemoryStream())
        //    {
        //Legacy comment translated to English.
        //        new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter().Serialize(ms, value);

        //        return new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Length);
        //    }
        //}

        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        ///// <param name="key">xxx*</param>
        ///// <returns></returns>
        //protected virtual IList<TValue> GetObjectList(TKey key)
        //{
        //    if (key == null)
        //    {
        //        return null;
        //    }
        //    var finalCacheKey = GetFinalCacheKey(key);
        //    var allData =  base.Cache.GetAll();

        //    if (allData.ContainsKey(finalCacheKey))
        //    {
        //        return allData[finalCacheKey]
        //    }
        //    return 
        //}

        /// <summary>
        /// Initializes a dictionary cache using the supplied logical cache key, database dependency,
        /// and entry lifetime.
        /// </summary>
        /// <param name="CACHE_KEY">The logical cache key used as the namespace for entries.</param>
        /// <param name="db">An optional database abstraction used by derived implementations.</param>
        /// <param name="timeOut">The default cache lifetime in minutes.</param>
        public BaseDictionaryCache(string CACHE_KEY, INcfDbData db, int timeOut)
            : base(CACHE_KEY, db)
        {
            base.TimeOut = timeOut;

            //Legacy comment translated to English.
            //var load = base.Data;
            //var finalCacheKey = GetFinalCacheKey(key);
            //if (base.Cache.CheckExisted(finalCacheKey))
            //{

            //}
        }

        /// <inheritdoc/>
        public override TValue Update()
        {
            return base.Update();
        }

        /// <summary>
        /// Creates and caches a value for <paramref name="key"/> through the implementation-specific source.
        /// </summary>
        /// <param name="key">The typed key used to locate the value.</param>
        /// <returns>The created value.</returns>
        public abstract TValue InsertObjectToCache(TKey key);

        /// <summary>
        /// Creates a cache value from <paramref name="obj"/>, stores it under the derived key,
        /// and returns the stored value.
        /// </summary>
        /// <param name="key">The typed key used to build the final cache key.</param>
        /// <param name="obj">The source entity used to populate the value.</param>
        /// <returns>The cached value, or <see langword="null"/> when <paramref name="obj"/> is <see langword="null"/>.</returns>
        /// <exception cref="Exception">Thrown when entity-to-cache conversion or cache storage fails.</exception>
        public virtual TValue InsertObjectToCache(TKey key, TEntity obj)
        {
            if (obj == null)
            {
                return null;
            }

            TValue fullObj = new TValue();
            var finalCacheKey = GetFinalCacheKey(key);
            if (fullObj is IBaseFullEntity<TEntity>)
            {
                try
                {
                    (fullObj as BaseFullEntity<TEntity>).CreateEntity(obj);

                    //if (base.Data == null)
                    //{
                    //    throw new Exception("base.Data=null");
                    //}
                    base.Cache.Set(finalCacheKey, fullObj);
                    return fullObj;
                }
                catch (Exception ex)
                {
                    //    .With(ex.Message, base.Data, key, obj, base.Data == null, key == null, obj == null, base.Data.Count);

                    var msg = $"系统调试记录cache的一个bug。发生错误：{ex.Message}。再次访问base.Data=null：{base.Data == null}";
                    LogUtility.SystemLogger.Debug(msg, ex);
                    throw new Exception(msg, ex);
                }
            }
            else if (obj as TValue != null)
            {
                base.Cache.Set(finalCacheKey, obj as TValue);
                return obj as TValue;
            }

            base.Cache.Set(finalCacheKey, fullObj);
            return fullObj;
        }

        /// <summary>
        /// Gets an entry by its typed key, creating and caching it when the entry is absent.
        /// </summary>
        /// <param name="key">The typed key used to locate the value.</param>
        /// <returns>The cached or newly created value, or <see langword="null"/> for a null key.</returns>
        public virtual TValue GetObject(TKey key)
        {
            if (key == null)
            {
                return null;
            }

            var finalCacheKey = GetFinalCacheKey(key);

            if (base.Cache.CheckExisted(finalCacheKey))
            {
                return base.Cache.Get<TValue>(finalCacheKey);
            }
            else
            {
                return InsertObjectToCache(key);
            }
        }

        /// <summary>
        /// Removes the entry associated with the supplied typed key when it exists.
        /// </summary>
        /// <param name="key">The typed key used to locate the entry.</param>
        public virtual void RemoveObject(TKey key)
        {
            var finalCacheKey = GetFinalCacheKey(key);

            if (base.Cache.CheckExisted(finalCacheKey))
            {
                base.Cache.RemoveFromCache(finalCacheKey);
            }
        }

        /// <summary>
        /// Stores <paramref name="obj"/> under the cache key derived from <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The typed key used to build the final cache key.</param>
        /// <param name="obj">The value to store.</param>
        /// <returns><see langword="true"/> when the underlying cache write reports success.</returns>
        public virtual bool UpdateToCache(TKey key, TValue obj)
        {
            var finalKey = GetFinalCacheKey(key);
            return base.UpdateToCache(finalKey, obj);
        }


        /// <summary>
        /// Disallows removal through the single-value cache API because dictionary entries
        /// must be addressed by their individual keys.
        /// </summary>
        /// <exception cref="Exception">Always thrown when this method is called.</exception>
        public override void RemoveCache()
        {
            throw new Exception("不可以使用此方法");
        }

        /// <inheritdoc/>
        public override async Task<TValue> UpdateAsync()
        {
            return await base.UpdateAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously creates and caches a value for <paramref name="key"/> through the implementation-specific source.
        /// </summary>
        /// <param name="key">The typed key used to locate the value.</param>
        /// <returns>A task whose result is the created value.</returns>
        public abstract Task<TValue> InsertObjectToCacheAsync(TKey key);

        /// <summary>
        /// Asynchronously creates a cache value from <paramref name="obj"/>, stores it under the derived key,
        /// and returns the stored value.
        /// </summary>
        /// <param name="key">The typed key used to build the final cache key.</param>
        /// <param name="obj">The source entity used to populate the value.</param>
        /// <returns>A task whose result is the cached value, or <see langword="null"/> for a null source entity.</returns>
        /// <exception cref="Exception">Thrown when entity-to-cache conversion or cache storage fails.</exception>
        public virtual async Task<TValue> InsertObjectToCacheAsync(TKey key, TEntity obj)
        {
            if (obj == null)
            {
                return null;
            }

            TValue fullObj = new TValue();
            var finalCacheKey = GetFinalCacheKey(key);
            if (fullObj is IBaseFullEntity<TEntity>)
            {
                try
                {
                    (fullObj as BaseFullEntity<TEntity>).CreateEntity(obj);

                    //if (base.Data == null)
                    //{
                    //    throw new Exception("base.Data=null");
                    //}
                    await base.Cache.SetAsync(finalCacheKey, fullObj).ConfigureAwait(false);
                    return fullObj;
                }
                catch (Exception ex)
                {
                    //    .With(ex.Message, base.Data, key, obj, base.Data == null, key == null, obj == null, base.Data.Count);

                    var msg = $"系统调试记录cache的一个bug。发生错误：{ex.Message}。再次访问base.Data=null：{base.Data == null}";
                    LogUtility.SystemLogger.Debug(msg, ex);
                    throw new Exception(msg, ex);
                }
            }
            else if (obj as TValue != null)
            {
                await base.Cache.SetAsync(finalCacheKey, obj as TValue).ConfigureAwait(false);
                return obj as TValue;
            }

            await base.Cache.SetAsync(finalCacheKey, fullObj).ConfigureAwait(false);
            return fullObj;
        }

        /// <summary>
        /// Asynchronously gets an entry by its typed key, creating and caching it when absent.
        /// </summary>
        /// <param name="key">The typed key used to locate the value.</param>
        /// <returns>A task whose result is the cached or newly created value, or <see langword="null"/> for a null key.</returns>
        public virtual async Task<TValue> GetObjectAsync(TKey key)
        {
            if (key == null)
            {
                return null;
            }

            var finalCacheKey = GetFinalCacheKey(key);

            if (base.Cache.CheckExisted(finalCacheKey))
            {
                return await base.Cache.GetAsync<TValue>(finalCacheKey).ConfigureAwait(false);
            }
            else
            {
                return await InsertObjectToCacheAsync(key).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Asynchronously removes the entry associated with the supplied typed key when it exists.
        /// </summary>
        /// <param name="key">The typed key used to locate the entry.</param>
        /// <returns>A task that completes after the entry has been removed.</returns>
        public virtual async Task RemoveObjectAsync(TKey key)
        {
            var finalCacheKey = GetFinalCacheKey(key);

            if (await base.Cache.CheckExistedAsync(finalCacheKey).ConfigureAwait(false))
            {
                await base.Cache.RemoveFromCacheAsync(finalCacheKey).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Asynchronously stores <paramref name="obj"/> under the cache key derived from <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The typed key used to build the final cache key.</param>
        /// <param name="obj">The value to store.</param>
        /// <returns>A task whose result indicates whether the underlying cache write reports success.</returns>
        public virtual async Task<bool> UpdateToCacheAsync(TKey key, TValue obj)
        {
            var finalKey = GetFinalCacheKey(key);
            return await base.UpdateToCacheAsync(finalKey, obj).ConfigureAwait(false);
        }


        /// <summary>
        /// Disallows removal through the single-value cache API because dictionary entries
        /// must be addressed by their individual keys.
        /// </summary>
        /// <exception cref="Exception">Always thrown when this method is called.</exception>
        public override Task RemoveCacheAsync()
        {
            throw new Exception("不可以使用此方法");
        }

    }
}
