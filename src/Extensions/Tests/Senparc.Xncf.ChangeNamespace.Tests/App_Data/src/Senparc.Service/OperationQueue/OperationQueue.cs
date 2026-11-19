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


    public class OperationQueue
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private static Dictionary<string, OperationQueueItem> MessageQueueDictionary = new Dictionary<string, OperationQueueItem>(StringComparer.OrdinalIgnoreCase);

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
        /// getOperationQueueItem
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
        ///Legacy comment translated to English.
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
                //Legacy comment translated to English.
                //}

                var mqItem = new OperationQueueItem(key, operationQueueType, data, description);
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
