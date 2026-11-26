using System;
using System.Collections.Generic;
using System.Linq;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Log;
using Senparc.Ncf.Utility;

namespace Senparc.Service.OperationQueue
{
    public enum OperationQueueType
    {
        更新用户头像,
        活动消息记录
    }
    /// <summary>
    /// Provides a process-local keyed queue for deferred account operations.
    /// </summary>
    public class OperationQueue
    {
        /// <summary>Stores queued operations by their case-insensitive composite key.</summary>
        private static Dictionary<string, OperationQueueItem> MessageQueueDictionary = new Dictionary<string, OperationQueueItem>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Serializes access to the queued-operation dictionary.</summary>
        private static object MessageQueueSyncLock = new object();
        /// <summary>Serializes queue flushing so only one worker drains it at a time.</summary>
        private static object FlushCacheLock = new object();

        /// <summary>
        /// Builds a stable queue key from an operation name, sender type,
        /// identity value, and action name.
        /// </summary>
        /// <param name="name">The logical operation name.</param>
        /// <param name="senderType">The type that originated the operation.</param>
        /// <param name="identityKey">The business identity used to coalesce operations.</param>
        /// <param name="actionName">The action being deferred.</param>
        /// <returns>The composite key used by the in-memory queue.</returns>
        public static string GenerateKey(string name, Type senderType, string identityKey, string actionName)
        {
            var key = string.Format("Name@{0}||Type@{1}||Key@{2}||ActionName@{3}",
                name, senderType, identityKey, actionName);
            return key;
        }

        /// <summary>
        /// Drains queued operations and dispatches supported operation types.
        /// Individual failures are logged and skipped so later items can continue.
        /// </summary>
        public static void OperateQueue()
        {
            lock (FlushCacheLock)
            {
                var mq = new OperationQueue();
                var key = mq.GetCurrentKey(); //Legacy comment translated to English.
                while (!string.IsNullOrEmpty(key))
                {
                    try
                    {
                        var operationQueueService = SenparcDI.GetService<OperationQueueService>();
                        var mqItem = mq.GetItem(key); //Legacy comment translated to English.

                        //Legacy comment translated to English.
                        switch (mqItem.OperationQueueType)
                        {
                            case OperationQueueType.更新用户头像:
                                operationQueueService.UpdateAccountHeadImgAsync((int)mqItem.Data[0], mqItem.Data[1] as string).Wait();
                                break;
                            default:
                                LogUtility.OperationQueue.ErrorFormat("OperationQueueType未处理：{0}", mqItem.OperationQueueType.ToString());
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogUtility.OperationQueue.ErrorFormat($"OperationQueue列队操作失败，已经跳过：{ex.Message}", ex);
                    }

                    mq.Remove(key); //Legacy comment translated to English.
                    key = mq.GetCurrentKey(); //Legacy comment translated to English.
                }
            }
        }

        /// <summary>Gets the first currently queued operation key.</summary>
        /// <returns>The first key, or <see langword="null"/> when the queue is empty.</returns>
        public string GetCurrentKey()
        {
            lock (MessageQueueSyncLock)
            {
                return MessageQueueDictionary.Keys.FirstOrDefault();
            }
        }

        /// <summary>Gets a queued operation by its composite key.</summary>
        /// <param name="key">The key returned by <see cref="GenerateKey"/>.</param>
        /// <returns>The queued item, or <see langword="null"/> when no item exists.</returns>
        public OperationQueueItem GetItem(string key)
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

        /// <summary>Adds or replaces a queued operation under a composite key.</summary>
        /// <param name="key">The key used to coalesce the operation.</param>
        /// <param name="operationQueueType">The operation dispatcher type.</param>
        /// <param name="data">The operation arguments consumed by the dispatcher.</param>
        /// <param name="description">Optional diagnostic text.</param>
        /// <returns>The newly stored queue item.</returns>
        public OperationQueueItem Add(string key, OperationQueueType operationQueueType, List<object> data, string description = null)
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

                var mqItem = new OperationQueueItem(key, operationQueueType, data, description);
                MessageQueueDictionary[key] = mqItem;
                return mqItem;
            }
        }

        /// <summary>Removes a queued operation when its key is present.</summary>
        /// <param name="key">The queued operation key.</param>
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

        /// <summary>Gets the number of currently queued operations.</summary>
        /// <returns>The number of items in the process-local queue.</returns>
        public int GetCount()
        {
            lock (MessageQueueSyncLock)
            {
                return MessageQueueDictionary.Count;
            }
        }

    }
}
