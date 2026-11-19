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
