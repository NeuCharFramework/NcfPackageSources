using Senparc.Xncf.Accounts.Domain.OperationQueue;
using System;
using System.Collections.Generic;

namespace Senparc.Xncf.Accounts.Domain.OperationQueue
{
    /// <summary>
    ///Operation queue item
    /// </summary>
    public class OperationQueueItem
    {
        /// <summary>
        /// Queue item unique identifier
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// Delegation executed when the queue item is hit and triggered
        /// </summary>
        public OperationQueueType OperationQueueType { get; set; }
        /// <summary>
        /// The creation time of this instance object
        /// </summary>
        public DateTime AddTime { get; set; }

        /// <summary>
        ///save data
        /// </summary>
        public List<object> Data { get; set; }

        /// <summary>
        ///Project description (mainly used for debugging)
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Initialize SenparcMessageQueue message queue items
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
