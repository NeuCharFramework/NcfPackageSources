/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: OperationQueueItem.cs
    File description: OperationQueueItem related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20261119
    Change description: v0.3.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Xncf.Accounts.Domain.OperationQueue;
using System;
using System.Collections.Generic;

namespace Senparc.Xncf.Accounts.Domain.OperationQueue
{
    /// <summary>Represents one deferred account operation and its arguments.</summary>
    public class OperationQueueItem
    {
        /// <summary>Gets or sets the composite queue key.</summary>
        public string Key { get; set; }
        /// <summary>Gets or sets the operation dispatcher type.</summary>
        public OperationQueueType OperationQueueType { get; set; }
        /// <summary>Gets or sets the enqueue timestamp.</summary>
        public DateTime AddTime { get; set; }

        /// <summary>Gets or sets the dispatcher arguments.</summary>
        public List<object> Data { get; set; }

        /// <summary>Gets or sets optional diagnostic text.</summary>
        public string Description { get; set; }

        /// <summary>Initializes a queued operation and records its creation time.</summary>
        /// <param name="key">The composite queue key.</param>
        /// <param name="operationQueueType">The operation type.</param>
        /// <param name="data">The dispatcher arguments.</param>
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
