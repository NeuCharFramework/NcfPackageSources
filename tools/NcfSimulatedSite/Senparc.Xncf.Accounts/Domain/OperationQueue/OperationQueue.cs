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
    public enum OperationQueueType
    {
        更新用户头像,
        活动消息记录
    }


    public class OperationQueue
    {
        /// <summary>
        ///Queue data collection
        /// </summary>
        private static Dictionary<string, OperationQueueItem> MessageQueueDictionary = new Dictionary<string, OperationQueueItem>(StringComparer.OrdinalIgnoreCase);

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
                var mq = new OperationQueue();
                var key = mq.GetCurrentKey(); //Get the latest Key
                var serviceProvider = SenparcDI.GetServiceProvider();
                while (!string.IsNullOrEmpty(key))
                {
                    try
                    {

                        var operationQueueService = serviceProvider.GetService<OperationQueueService>();
                        var mqItem = mq.GetItem(key); //Get task items

                        //Identification type
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
        /// Get OperationQueueItem
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
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

        /// <summary>
        ///Add queue members
        /// </summary>
        /// <param name="key"></param>
        /// <param name="action"></param>
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
                //    MessageQueueList.Add(key);//Move to the end
                //}

                var mqItem = new OperationQueueItem(key, operationQueueType, data, description);
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
