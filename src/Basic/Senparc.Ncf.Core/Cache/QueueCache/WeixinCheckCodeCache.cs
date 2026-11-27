/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinCheckCodeCache.cs
    File description: WeixinCheckCodeCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache.QueueCache
{
    //public interface IWeixinCheckCodeCache : IQueueCache<QueueCacheData<string>>
    //{
    //}

    [Serializable]
    public class WeixinCheckCodeCache : QueueCache<QueueCacheData<string>>/*, IWeixinCheckCodeCache*/
    {
        private const string cacheKey = "WeixinCheckCodeCache";
        private const int timeoutSeconds = 3 * 60;
        /// <summary>
        /// Initializes the Weixin verification-code queue cache with a three-minute timeout.
        /// </summary>
        public WeixinCheckCodeCache()
            : base(cacheKey, timeoutSeconds)
        {

        }

        public override QueueCacheData<QueueCacheData<string>> Get(string key, bool removeDataWhenExist = true)
        {
            var value = base.Get(key, removeDataWhenExist);
            if (value != null)
            {
                base.Remove(key);//Legacy comment translated to English.
            }
            return value;
        }
    }
}
