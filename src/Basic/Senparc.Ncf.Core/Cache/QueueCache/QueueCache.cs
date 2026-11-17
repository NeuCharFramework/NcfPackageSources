/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: QueueCache.cs

    File description: QueueCache related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Senparc.CO2NET.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Cache
{
    public interface IQueueCache<T>
    {
        List<QueueCacheData<T>> MessageQueue { get; set; }
        Dictionary<string, QueueCacheData<T>> MessageCollection { get; set; }
        string CreateKey();
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="obj"></param>
        ///Legacy comment translated to English.
        /// <returns></returns>
        string Insert(T obj, string key);
        QueueCacheData<T> Get(string key, bool removeDataWhenExist = true);
        void Remove(string key);
    }

    [Serializable]
    public class QueueCache<T> : IQueueCache<T>
    {
        private string _cacheKey;
        private int _timeoutSeconds;
        public List<QueueCacheData<T>> MessageQueue { get; set; }
        public Dictionary<string, QueueCacheData<T>> MessageCollection { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheKey"></param>
        ///Legacy comment translated to English.
        protected QueueCache(string cacheKey, int timeoutSeconds)
        {
            _cacheKey = cacheKey;
            _timeoutSeconds = timeoutSeconds;
        }

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
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="key">user name（OpenId）</param>
        ///Legacy comment translated to English.
        /// <returns></returns>
        private QueueCacheData<T> GetMessageContext(string key, bool removeDataWhenExist = true)
        {
            //Legacy comment translated to English.
            while (MessageQueue.Count > 0)
            {
                var firstMessageContext = MessageQueue[0];
                var timeSpan = DateTime.Now - firstMessageContext.LastActiveTime;
                if (removeDataWhenExist && _timeoutSeconds >= 0 && timeSpan.TotalSeconds >= _timeoutSeconds)
                {
                    MessageQueue.RemoveAt(0);//Legacy comment translated to English.
                    MessageCollection.Remove(firstMessageContext.Key);//Legacy comment translated to English.
                }
                else
                {
                    break;
                }
            }

            /* 

             * Legacy comment translated to English.

             * Legacy comment translated to English.

             * Legacy comment translated to English.

             */
            if (!MessageCollection.ContainsKey(key))
            {
                return null;
            }

            return MessageCollection[key];
        }

        private QueueCacheData<T> GetMessageContext_CreateIfNotExists(string key)
        {
            var messageContext = GetMessageContext(key);

            if (messageContext == null)
            {
                //Legacy comment translated to English.
                MessageCollection[key] = new QueueCacheData<T>(key);
                messageContext = GetMessageContext(key);
                //Legacy comment translated to English.
                MessageQueue.Add(messageContext); //Legacy comment translated to English.
            }
            return messageContext;
        }

        public virtual string CreateKey()
        {
            return Guid.NewGuid().ToString("n").Substring(0, 16);
        }

        public virtual string Insert(T obj, string key)
        {
            //Legacy comment translated to English.
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
                //Legacy comment translated to English.
                MessageQueue.RemoveAt(messageContextInQueue); //Legacy comment translated to English.
                MessageQueue.Add(messageContext); //Legacy comment translated to English.
            }

            messageContext.LastActiveTime = DateTime.Now;//Legacy comment translated to English.
            return key;
        }

        public virtual QueueCacheData<T> Get(string key, bool removeDataWhenExist = true)
        {
            return GetMessageContext(key, removeDataWhenExist);
        }

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
