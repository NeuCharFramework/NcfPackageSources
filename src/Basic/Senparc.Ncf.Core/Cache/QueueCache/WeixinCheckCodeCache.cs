/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: WeixinCheckCodeCache.cs

    File description: WeixinCheckCodeCache related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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
