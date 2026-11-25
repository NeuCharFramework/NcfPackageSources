/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: QueueCache.cs
    File description: QueueCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Senparc.CO2NET.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Cache
{
    /// <summary>
    /// Defines a short-lived, keyed queue of values whose entries can be retrieved or removed by key.
    /// </summary>
    /// <typeparam name="T">The type of data held by each queue entry.</typeparam>
    public interface IQueueCache<T>
    {
        /// <summary>Gets or sets the ordered queue of cache entries.</summary>
        List<QueueCacheData<T>> MessageQueue { get; set; }
        /// <summary>Gets or sets the key lookup table for cache entries.</summary>
        Dictionary<string, QueueCacheData<T>> MessageCollection { get; set; }
        /// <summary>Creates a unique key suitable for a new queue entry.</summary>
        string CreateKey();
        /// <summary>
        /// Inserts a value and returns the key assigned to the resulting entry.
        /// </summary>
        /// <param name="obj">The value to associate with the entry.</param>
        /// <param name="key">An optional preferred key; a new key is generated when it is empty or already used.</param>
        /// <returns>The key assigned to the inserted value.</returns>
        string Insert(T obj, string key);
        /// <summary>Gets an entry by key, optionally expiring entries encountered during the lookup.</summary>
        /// <param name="key">The key assigned to the entry.</param>
        /// <param name="removeDataWhenExist">Whether expired entries should be removed while searching.</param>
        /// <returns>The matching entry, or <see langword="null"/> when no entry exists.</returns>
        QueueCacheData<T> Get(string key, bool removeDataWhenExist = true);
        /// <summary>Removes the entry associated with a key.</summary>
        /// <param name="key">The key assigned to the entry.</param>
        void Remove(string key);
    }

    /// <summary>
    /// Maintains an in-memory queue and keyed collection for short-lived values.
    /// </summary>
    /// <typeparam name="T">The type of data held by each queue entry.</typeparam>
    [Serializable]
    public class QueueCache<T> : IQueueCache<T>
    {
        private string _cacheKey;
        private int _timeoutSeconds;
        public List<QueueCacheData<T>> MessageQueue { get; set; }
        public Dictionary<string, QueueCacheData<T>> MessageCollection { get; set; }

        /// <summary>
        /// Initializes a queue cache using the backing cache namespace and entry timeout.
        /// </summary>
        /// <param name="cacheKey">The cache namespace used to persist the queue and lookup table.</param>
        /// <param name="timeoutSeconds">The inactivity timeout for entries, in seconds.</param>
        protected QueueCache(string cacheKey, int timeoutSeconds)
        {
            _cacheKey = cacheKey;
            _timeoutSeconds = timeoutSeconds;
        }

        /// <summary>
        /// Creates and initializes a queue cache by loading its queue and key collection
        /// from the shared method cache.
        /// </summary>
        /// <param name="cacheKey">The cache namespace used to persist the queue and lookup table.</param>
        /// <param name="timeoutSeconds">The inactivity timeout for entries, in seconds.</param>
        /// <returns>A task whose result is an initialized queue cache.</returns>
        public static async Task<QueueCache<T>> CreateAsync(string cacheKey, int timeoutSeconds)
        {
            var instance = new QueueCache<T>(cacheKey, timeoutSeconds);
            instance.MessageQueue = await MethodCache.GetMethodCacheAsync(
                cacheKey + "Queue",
                 () => Task.FromResult(new List<QueueCacheData<T>>()),
                timeoutSeconds);

            instance.MessageCollection = await MethodCache.GetMethodCacheAsync(
                cacheKey + "Collection",
                 () => Task.FromResult(new Dictionary<string, QueueCacheData<T>>(StringComparer.OrdinalIgnoreCase)),
                timeoutSeconds);

            return instance;
        }

        /// <summary>
        /// Finds an entry by key after removing expired entries from the head of the queue.
        /// </summary>
        /// <param name="key">The key assigned to the entry.</param>
        /// <param name="removeDataWhenExist">Whether expired entries should be removed before the lookup.</param>
        /// <returns>The matching entry, or <see langword="null"/> when the key is not present.</returns>
        private QueueCacheData<T> GetMessageContext(string key, bool removeDataWhenExist = true)
        {
            while (MessageQueue.Count > 0)
            {
                var firstMessageContext = MessageQueue[0];
                var timeSpan = DateTime.Now - firstMessageContext.LastActiveTime;
                if (removeDataWhenExist && _timeoutSeconds >= 0 && timeSpan.TotalSeconds >= _timeoutSeconds)
                {
                    MessageQueue.RemoveAt(0);
                    MessageCollection.Remove(firstMessageContext.Key);
                }
                else
                {
                    break;
                }
            }

            if (!MessageCollection.ContainsKey(key))
            {
                return null;
            }

            return MessageCollection[key];
        }

        /// <summary>
        /// Gets an existing entry or creates an empty entry when the key is not present.
        /// </summary>
        /// <param name="key">The key assigned to the entry.</param>
        /// <returns>The existing or newly created entry.</returns>
        private QueueCacheData<T> GetMessageContext_CreateIfNotExists(string key)
        {
            var messageContext = GetMessageContext(key);

            if (messageContext == null)
            {
                MessageCollection[key] = new QueueCacheData<T>(key);
                messageContext = GetMessageContext(key);
                MessageQueue.Add(messageContext);
            }
            return messageContext;
        }

        /// <summary>
        /// Generates a new key for a queue entry.
        /// </summary>
        /// <returns>A key generated for a new queue entry.</returns>
        public virtual string CreateKey()
        {
            return Guid.NewGuid().ToString("n").Substring(0, 16);
        }

        /// <summary>
        /// Inserts or updates a value and moves the corresponding entry to the active end of the queue.
        /// </summary>
        /// <param name="obj">The value to store.</param>
        /// <param name="key">An optional preferred key; a new key is generated when it is empty or already used.</param>
        /// <returns>The key assigned to the value.</returns>
        public virtual string Insert(T obj, string key)
        {
            //while (MessageQueue.Count > 0)
            //{
            //    var firstMessageContext = MessageQueue[0];
            //    var timeSpan = DateTime.Now - firstMessageContext.LastActiveTime;
            //    if (timeSpan.TotalSeconds >= _timeoutSeconds)
            //    {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //    }
            //    else
            //    {
            //        break;
            //    }
            //}

            while (key.IsNullOrEmpty() || MessageCollection.ContainsKey(key))
            {
                key = CreateKey();
            }

            var messageContext = GetMessageContext_CreateIfNotExists(key);
            messageContext.Data = obj;

            var messageContextInQueue = MessageQueue.IndexOf(messageContext);
            if (MessageQueue.Count > 1 && messageContextInQueue != MessageQueue.Count - 1)
            {
                MessageQueue.RemoveAt(messageContextInQueue);
                MessageQueue.Add(messageContext);
            }

            messageContext.LastActiveTime = DateTime.Now;//Legacy comment translated to English.
            return key;
        }

        /// <summary>
        /// Gets the active entry associated with a key.
        /// </summary>
        /// <param name="key">The key assigned to the entry.</param>
        /// <param name="removeDataWhenExist">Whether expired entries should be removed before the lookup.</param>
        /// <returns>The matching entry, or <see langword="null"/> when it is absent.</returns>
        public virtual QueueCacheData<T> Get(string key, bool removeDataWhenExist = true)
        {
            return GetMessageContext(key, removeDataWhenExist);
        }

        /// <summary>
        /// Removes a queue entry. Cleanup exceptions are intentionally suppressed to preserve
        /// the cache's best-effort semantics.
        /// </summary>
        /// <param name="key">The key assigned to the entry.</param>
        public virtual void Remove(string key)
        {
            try
            {
                var messageContext = GetMessageContext_CreateIfNotExists(key);
                MessageQueue.Remove(messageContext);
                MessageCollection.Remove(key);
            }
            catch (Exception)
            {
            }
        }
    }
}
