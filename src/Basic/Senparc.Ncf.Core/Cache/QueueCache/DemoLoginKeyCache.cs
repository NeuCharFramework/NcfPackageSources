/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: DemoLoginKeyCache.cs

    File description: DemoLoginKeyCache related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Cache
{
    [Serializable]
    public class DemoLoginKeyCacheData
    {
        public string OpenId { get; set; }
        public DateTime AddTime { get; set; }
        public QrCodeLoginDataType QrCodeLoginDataType { get; set; }
        public DemoLoginKeyCacheData(string openId, QrCodeLoginDataType qrCodeLoginDataType)
        {
            OpenId = openId;
            QrCodeLoginDataType = qrCodeLoginDataType;
            AddTime = DateTime.Now;
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public interface IDemoLoginKeyCache : IQueueCache<DemoLoginKeyCacheData>
    {

    }

    [Serializable]
    public class DemoLoginKeyCache : QueueCache<DemoLoginKeyCacheData>, IDemoLoginKeyCache
    {
        private const string cacheKey = "DemoLoginKeyCache";
        private const int timeoutSeconds = 5 * 60;
        private DemoLoginKeyCache()
         : base(cacheKey, timeoutSeconds)
        {
        }

        public static async Task<DemoLoginKeyCache> CreateAsync()
        {
            var instance = new DemoLoginKeyCache();

            //Legacy comment translated to English.
            await instance.InitializeAsync();

            return instance;
        }

        private async Task InitializeAsync()
        {
            //Legacy comment translated to English.
            var queueCache = await QueueCache<DemoLoginKeyCacheData>.CreateAsync(cacheKey, timeoutSeconds);

            //Legacy comment translated to English.
            this.MessageQueue = queueCache.MessageQueue;
            this.MessageCollection = queueCache.MessageCollection;
        }

        public override QueueCacheData<DemoLoginKeyCacheData> Get(string key, bool removeDataWhenExist = true)
        {
            var value = base.Get(key,removeDataWhenExist);
            if (value != null)
            {
                base.Remove(key);//Legacy comment translated to English.
            }
            return value;
        }
    }
}
