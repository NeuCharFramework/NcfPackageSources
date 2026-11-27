/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: OperationQueue.cs
    File description: OperationQueue related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20261119
    Change description: v0.3.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.3.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Log;
using Senparc.Ncf.Utility;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Xncf.Accounts.Domain.OperationQueue
{
    /// <summary>Identifies deferred account operations handled by the queue.</summary>
    public enum OperationQueueType
    {
        更新用户头像,
        活动消息记录
    }

    /// <summary>Provides a process-local keyed queue for deferred account operations.</summary>
    public class OperationQueue
    {
        /// <summary>Stores queued operations by case-insensitive composite key.</summary>
        private static Dictionary<string, OperationQueueItem> MessageQueueDictionary = new Dictionary<string, OperationQueueItem>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Serializes access to the queue dictionary.</summary>
        private static object MessageQueueSyncLock = new object();
        /// <summary>Serializes queue draining.</summary>
        private static object FlushCacheLock = new object();

        /// <summary>Builds a stable key for a deferred operation.</summary>
        /// <param name="name">The operation name.</param>
        /// <param name="senderType">The source type.</param>
        /// <param name="identityKey">The business identity.</param>
        /// <param name="actionName">The action name.</param>
        /// <returns>The composite queue key.</returns>
        public static string GenerateKey(string name, Type senderType, string identityKey, string actionName)
        {
            var key = string.Format("Name@{0}||Type@{1}||Key@{2}||ActionName@{3}",
                name, senderType, identityKey, actionName);
            return key;
        }

        /// <summary>Drains and dispatches all currently queued operations.</summary>
        public static void OperateQueue()
        {
            lock (FlushCacheLock)
            {
                var mq = new OperationQueue();
                var key = mq.GetCurrentKey(); //Legacy comment translated to English.
                var serviceProvider = SenparcDI.GetServiceProvider();
                while (!string.IsNullOrEmpty(key))
                {
                    try
                    {

                        var operationQueueService = serviceProvider.GetService<OperationQueueService>();
                        var mqItem = mq.GetItem(key); //Legacy comment translated to English.

                        //Legacy comment translated to English.
                        switch (mqItem.OperationQueueType)
                        {
                            case OperationQueueType.更新用户头像:
                                operationQueueService.UpdateAccountHeadImgAsync(serviceProvider, (int)mqItem.Data[0], mqItem.Data[1] as string).Wait();
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

        /// <summary>Gets the first queued key, or null when the queue is empty.</summary>
        public string GetCurrentKey()
        {
            lock (MessageQueueSyncLock)
            {
                return MessageQueueDictionary.Keys.FirstOrDefault();
            }
        }

        /// <summary>Gets a queued item by key.</summary>
        /// <param name="key">The composite queue key.</param>
        /// <returns>The item, or null when not found.</returns>
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

        /// <summary>Adds or replaces a queued operation.</summary>
        /// <param name="key">The composite queue key.</param>
        /// <param name="operationQueueType">The operation type.</param>
        /// <param name="data">The dispatcher arguments.</param>
        /// <param name="description">Optional diagnostic text.</param>
        /// <returns>The stored queue item.</returns>
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

        /// <summary>Removes a queued operation by key.</summary>
        /// <param name="key">The composite queue key.</param>
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

        /// <summary>Gets the number of queued operations.</summary>
        /// <returns>The current queue size.</returns>
        public int GetCount()
        {
            lock (MessageQueueSyncLock)
            {
                return MessageQueueDictionary.Count;
            }
        }

    }
}
