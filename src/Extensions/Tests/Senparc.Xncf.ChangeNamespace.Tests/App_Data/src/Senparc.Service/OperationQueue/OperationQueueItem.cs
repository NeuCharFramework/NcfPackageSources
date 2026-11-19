using Senparc.Service.OperationQueue;
using System;
using System.Collections.Generic;

namespace Senparc.Service.OperationQueue
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class OperationQueueItem
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public OperationQueueType OperationQueueType { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime AddTime { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public List<object> Data { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="description"></param>
        /// <param name="operationQueueType"></param>
        public OperationQueueItem(string key, OperationQueueType operationQueueType, List<object> data, string description = null)
        {
            Key = key;
            OperationQueueType = operationQueueType;
            Data = data;
            Description = description;
            AddTime = DateTime.Now;
        }
    }
}
