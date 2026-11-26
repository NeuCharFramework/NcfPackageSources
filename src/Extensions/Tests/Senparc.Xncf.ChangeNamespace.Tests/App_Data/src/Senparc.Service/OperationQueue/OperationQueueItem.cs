using Senparc.Service.OperationQueue;
using System;
using System.Collections.Generic;

namespace Senparc.Service.OperationQueue
{
    /// <summary>
    /// Represents one deferred operation and its dispatcher arguments.
    /// </summary>
    public class OperationQueueItem
    {
        /// <summary>Gets or sets the composite queue key.</summary>
        public string Key { get; set; }
        /// <summary>Gets or sets the dispatcher operation type.</summary>
        public OperationQueueType OperationQueueType { get; set; }
        /// <summary>Gets or sets the time at which the item was queued.</summary>
        public DateTime AddTime { get; set; }

        /// <summary>Gets or sets the dispatcher arguments.</summary>
        public List<object> Data { get; set; }

        /// <summary>Gets or sets optional diagnostic text.</summary>
        public string Description { get; set; }

        /// <summary>Initializes a queued operation and records its creation time.</summary>
        /// <param name="key">The composite queue key.</param>
        /// <param name="operationQueueType">The dispatcher operation type.</param>
        /// <param name="data">The arguments consumed by the dispatcher.</param>
        /// <param name="description">Optional diagnostic text.</param>
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
