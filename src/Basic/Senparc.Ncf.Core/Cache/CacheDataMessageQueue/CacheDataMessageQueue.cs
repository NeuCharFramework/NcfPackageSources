using System;
using System.Collections.Generic;
using System.Linq;

namespace Senparc.Ncf.Core.Cache
{
    /// <summary>
    /// Cache message queue, used to automatically update distributed cache data
    /// </summary>
    public class CacheDataMessageQueue
    {
        /// <summary>
        ///Queue data collection
        /// </summary>
        private static Dictionary<string, CacheDataMessageQueueItem> MessageQueueDictionary = new Dictionary<string, CacheDataMessageQueueItem>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Synchronous execution lock
        /// </summary>
        private static object MessageQueueSyncLock = new object();
        /// <summary>
        /// Immediately synchronize all cache execution locks (used by OperateQueue())
        /// </summary>
        private static object FlushCacheLock = new object();

        /// <summary>
        /// Generate Key
        /// </summary>
        /// <param name="name">Queue application name, such as "ContainerBag"</param>
        /// <param name="senderType">Operation object type</param>
        /// <param name="identityKey">Object unique identification Key</param>
        /// <param name="actionName">Action name, such as "UpdateContainerBag"</param>
        /// <returns></returns>
        public static string GenerateKey(string name, Type senderType, string identityKey, string actionName)
        {
            var key = string.Format("Name@{0}||Type@{1}||Key@{2}||ActionName@{3}",
                name, senderType, identityKey, actionName);
            return key;
        }

        /// <summary>
        ///Operation queue
        /// </summary>
        public static void OperateQueue()
        {
            lock (FlushCacheLock)
            {
                var mq = new CacheDataMessageQueue();
                var key = mq.GetCurrentKey(); //Get the latest Key
                while (!string.IsNullOrEmpty(key))
                {
                    var mqItem = mq.GetItem(key); //Get task items
                    mqItem.Action(); //implement
                    mq.Remove(key); //Clear
                    key = mq.GetCurrentKey(); //Get the latest Key
                }
            }
        }

        /// <summary>
        /// Get the Key currently waiting for execution
        /// </summary>
        /// <returns></returns>
        public string GetCurrentKey()
        {
            lock (MessageQueueSyncLock)
            {
                return MessageQueueDictionary.Keys.FirstOrDefault();
            }
        }

        /// <summary>
        /// Get SenparcMessageQueueItem
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public CacheDataMessageQueueItem GetItem(string key)
        {
            lock (MessageQueueSyncLock)
            {
                if (MessageQueueDictionary.ContainsKey(key))
                {
                    return MessageQueueDictionary[key];
                }
                return null;
            }
        }

        /// <summary>
        ///Add queue members
        /// </summary>
        /// <param name="key"></param>
        /// <param name="action"></param>
        public CacheDataMessageQueueItem Add(string key, Action action)
        {
            lock (MessageQueueSyncLock)
            {
                //if (!MessageQueueDictionary.ContainsKey(key))
                //{
                //    MessageQueueList.Add(key);
                //}
                //else
                //{
                //    MessageQueueList.Remove(key);
                //    MessageQueueList.Add(key);//Move to the end
                //}

                var mqItem = new CacheDataMessageQueueItem(key, action);
                MessageQueueDictionary[key] = mqItem;
                return mqItem;
            }
        }

        /// <summary>
        ///Remove queue members
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            lock (MessageQueueSyncLock)
            {
                if (MessageQueueDictionary.ContainsKey(key))
                {
                    MessageQueueDictionary.Remove(key);
                    //MessageQueueList.Remove(key);
                }
            }
        }

        /// <summary>
        /// Get the current queue number
        /// </summary>
        /// <returns></returns>
        public int GetCount()
        {
            lock (MessageQueueSyncLock)
            {
                return MessageQueueDictionary.Count;
            }
        }

    }
}
