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
    /// Login permission cache (cache data: UserId)
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

            // Initialize the asynchronous initialization operation in the parent class  
            await instance.InitializeAsync();

            return instance;
        }

        private async Task InitializeAsync()
        {
            // Call the static factory method of the parent class for asynchronous initialization  
            var queueCache = await QueueCache<DemoLoginKeyCacheData>.CreateAsync(cacheKey, timeoutSeconds);

            // Assign the initialization result to the current instance  
            this.MessageQueue = queueCache.MessageQueue;
            this.MessageCollection = queueCache.MessageCollection;
        }

        public override QueueCacheData<DemoLoginKeyCacheData> Get(string key, bool removeDataWhenExist = true)
        {
            var value = base.Get(key,removeDataWhenExist);
            if (value != null)
            {
                base.Remove(key);  // One-time use
            }
            return value;
        }
    }
}
