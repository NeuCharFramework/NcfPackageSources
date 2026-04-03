using System;

namespace Senparc.Ncf.Core.Cache
{
    /// <summary>
    ///CacheDataMessageQueue message queue entry
    /// </summary>
    public class CacheDataMessageQueueItem
    {
        /// <summary>
        /// Unique identifier of queue item
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// Delegation executed when the queue item is hit and triggered
        /// </summary>
        public Action Action { get; set; }
        /// <summary>
        /// The creation time of this instance object
        /// </summary>
        public DateTime AddTime { get; set; }
        /// <summary>
        /// Item description (mainly for debugging)
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Initialize SenparcMessageQueue message queue items
        /// </summary>
        /// <param name="key"></param>
        /// <param name="action"></param>
        /// <param name="description"></param>
        public CacheDataMessageQueueItem(string key, Action action, string description = null)
        {
            Key = key;
            Action = action;
            Description = description;
            AddTime = DateTime.Now;
        }
    }
}
