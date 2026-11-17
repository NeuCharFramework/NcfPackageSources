/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: CacheDataMessageQueueItem.cs

    File description: CacheDataMessageQueueItem related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class CacheDataMessageQueueItem
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public Action Action { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime AddTime { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
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
