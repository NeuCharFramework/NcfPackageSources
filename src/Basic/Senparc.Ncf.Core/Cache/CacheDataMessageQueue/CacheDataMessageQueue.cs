/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: CacheDataMessageQueue.cs
    File description: CacheDataMessageQueue related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Senparc.Ncf.Core.Cache
{
    /// <summary>
    /// Stores deferred cache-related actions by a stable composite key and executes them
    /// in a serialized flush operation.
    /// </summary>
    public class CacheDataMessageQueue
    {
        /// <summary>
        /// Holds pending actions indexed by their composite queue key.
        /// </summary>
        private static Dictionary<string, CacheDataMessageQueueItem> MessageQueueDictionary = new Dictionary<string, CacheDataMessageQueueItem>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Protects reads and writes to <see cref="MessageQueueDictionary"/>.
        /// </summary>
        private static object MessageQueueSyncLock = new object();
        /// <summary>
        /// Prevents concurrent callers from flushing the same queue at the same time.
        /// </summary>
        private static object FlushCacheLock = new object();

        /// <summary>
        /// Builds the case-insensitive key used to coalesce or locate a deferred action.
        /// </summary>
        /// <param name="name">The logical name of the operation or message source.</param>
        /// <param name="senderType">The type that originated the operation.</param>
        /// <param name="identityKey">The identity of the affected cache entry or entity.</param>
        /// <param name="actionName">The operation name used to distinguish actions for the same identity.</param>
        /// <returns>A composite key containing all supplied identity components.</returns>
        public static string GenerateKey(string name, Type senderType, string identityKey, string actionName)
        {
            var key = string.Format("Name@{0}||Type@{1}||Key@{2}||ActionName@{3}",
                name, senderType, identityKey, actionName);
            return key;
        }

        /// <summary>
        /// Executes each currently queued action until the queue is empty.
        /// </summary>
        public static void OperateQueue()
        {
            lock (FlushCacheLock)
            {
                var mq = new CacheDataMessageQueue();
                var key = mq.GetCurrentKey();
                while (!string.IsNullOrEmpty(key))
                {
                    var mqItem = mq.GetItem(key);
                    mqItem.Action();
                    mq.Remove(key);
                    key = mq.GetCurrentKey();
                }
            }
        }

        /// <summary>
        /// Gets one currently queued key. The returned value is an available work item,
        /// not a durable sequence number.
        /// </summary>
        /// <returns>The first key returned by dictionary enumeration, or <see langword="null"/> when empty.</returns>
        public string GetCurrentKey()
        {
            lock (MessageQueueSyncLock)
            {
                return MessageQueueDictionary.Keys.FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets the queued action associated with a composite queue key.
        /// </summary>
        /// <param name="key">The key previously produced by <see cref="GenerateKey"/> or supplied to <see cref="Add"/>.</param>
        /// <returns>The queued item, or <see langword="null"/> when no item exists for <paramref name="key"/>.</returns>
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
        /// Adds or replaces the deferred action associated with a queue key.
        /// </summary>
        /// <param name="key">The key used to identify the deferred action.</param>
        /// <param name="action">The action to execute during <see cref="OperateQueue"/>.</param>
        /// <returns>The queue item created for the key.</returns>
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
                //}

                var mqItem = new CacheDataMessageQueueItem(key, action);
                MessageQueueDictionary[key] = mqItem;
                return mqItem;
            }
        }

        /// <summary>
        /// Removes a queued action when an item exists for the supplied key.
        /// </summary>
        /// <param name="key">The key of the queued action to remove.</param>
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
        /// Gets the number of actions currently waiting in the queue.
        /// </summary>
        /// <returns>The current number of queued actions.</returns>
        public int GetCount()
        {
            lock (MessageQueueSyncLock)
            {
                return MessageQueueDictionary.Count;
            }
        }

    }
}
