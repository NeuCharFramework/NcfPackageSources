/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: CacheDataMessageQueueItem.cs
    File description: CacheDataMessageQueueItem related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache
{
    /// <summary>
    /// Represents one deferred cache update action.
    /// </summary>
    public class CacheDataMessageQueueItem
    {
        /// <summary>
        /// Gets or sets the queue item key.
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// Gets or sets the deferred action.
        /// </summary>
        public Action Action { get; set; }
        /// <summary>
        /// Gets or sets the enqueue time.
        /// </summary>
        public DateTime AddTime { get; set; }
        /// <summary>
        /// Gets or sets the diagnostic description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Initializes a cache queue item.
        /// </summary>
        /// <param name="key">The cache update key.</param>
        /// <param name="action">The deferred cache update action.</param>
        /// <param name="description">Optional diagnostic text.</param>
        public CacheDataMessageQueueItem(string key, Action action, string description = null)
        {
            Key = key;
            Action = action;
            Description = description;
            AddTime = DateTime.Now;
        }
    }
}
