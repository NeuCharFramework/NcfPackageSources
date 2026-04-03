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
        ///insert cache
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="key">If it is null, a 16-bit Guid will be automatically generated</param>
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
        /// <param name="timeoutSeconds">If it is less than or equal to 0, there will be no expiration time</param>
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
        /// Get MessageContext, if it does not exist, return null
        /// The more important significance of this method is to operate the TM queue, remove expired information in time, and move the latest active objects to the end.
        /// </summary>
        /// <param name="key">Username (OpenId)</param>
        /// <param name="removeDataWhenExist">Whether to clear key</param>
        /// <returns></returns>
        private QueueCacheData<T> GetMessageContext(string key, bool removeDataWhenExist = true)
        {
            // Check and remove expired records. Temporary no standalone thread polling to save resources
            while (MessageQueue.Count > 0)
            {
                var firstMessageContext = MessageQueue[0];
                var timeSpan = DateTime.Now - firstMessageContext.LastActiveTime;
                if (removeDataWhenExist && _timeoutSeconds >= 0 && timeSpan.TotalSeconds >= _timeoutSeconds)
                {
                    MessageQueue.RemoveAt(0);//Remove expired objects from queue
                    MessageCollection.Remove(firstMessageContext.Key);//Remove expired objects from collection
                }
                else
                {
                    break;
                }
            }

            /* 
             *Global MessageCollection.ContainsKey is only used here
             * Fully separate the internal operations of MessageCollection,
             * Leave room for future changes or extensions to MessageCollection
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
                //Globally only use MessageCollection[Key] to write in this one place
                MessageCollection[key] = new QueueCacheData<T>(key);
                messageContext = GetMessageContext(key);
                //insert queue
                MessageQueue.Add(messageContext); //Newest at the end
            }
            return messageContext;
        }

        public virtual string CreateKey()
        {
            return Guid.NewGuid().ToString("n").Substring(0, 16);
        }

        public virtual string Insert(T obj, string key)
        {
            // Check and remove expired records. Temporary no standalone thread polling to save resources
            //while (MessageQueue.Count > 0)
            //{
            //    var firstMessageContext = MessageQueue[0];
            //    var timeSpan = DateTime.Now - firstMessageContext.LastActiveTime;
            //    if (timeSpan.TotalSeconds >= _timeoutSeconds)
            //    {
            //        MessageQueue.RemoveAt(0);//Remove expired objects from the queue
            //        MessageCollection.Remove(firstMessageContext.Key);//Remove expired objects from the collection
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
                //If it is not a new object, move the current object to the end of the queue (the new object is already at the bottom)
                MessageQueue.RemoveAt(messageContextInQueue); //Remove current object
                MessageQueue.Add(messageContext); //insert at end
            }

            messageContext.LastActiveTime = DateTime.Now;//Log request time
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
