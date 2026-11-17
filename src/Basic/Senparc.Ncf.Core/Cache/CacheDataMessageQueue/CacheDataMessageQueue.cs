/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: CacheDataMessageQueue.cs

    File description: CacheDataMessageQueue related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Senparc.Ncf.Core.Cache
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class CacheDataMessageQueue
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private static Dictionary<string, CacheDataMessageQueueItem> MessageQueueDictionary = new Dictionary<string, CacheDataMessageQueueItem>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private static object MessageQueueSyncLock = new object();
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private static object FlushCacheLock = new object();

        /// <summary>
        /// generateKey
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static string GenerateKey(string name, Type senderType, string identityKey, string actionName)
        {
            var key = string.Format("Name@{0}||Type@{1}||Key@{2}||ActionName@{3}",
                name, senderType, identityKey, actionName);
            return key;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static void OperateQueue()
        {
            lock (FlushCacheLock)
            {
                var mq = new CacheDataMessageQueue();
                var key = mq.GetCurrentKey(); //Legacy comment translated to English.
                while (!string.IsNullOrEmpty(key))
                {
                    var mqItem = mq.GetItem(key); //Legacy comment translated to English.
                    mqItem.Action(); //execute
                    mq.Remove(key); //Legacy comment translated to English.
                    key = mq.GetCurrentKey(); //Legacy comment translated to English.
                }
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
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
        /// getSenparcMessageQueueItem
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
        ///Legacy comment translated to English.
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
                //Legacy comment translated to English.
                //}

                var mqItem = new CacheDataMessageQueueItem(key, action);
                MessageQueueDictionary[key] = mqItem;
                return mqItem;
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
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
