/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DemoLoginKeyCache.cs
    File description: DemoLoginKeyCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

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
        /// <summary>
        /// Initializes login-key data with the associated OpenID and QR login type.
        /// </summary>
        /// <param name="openId">The user OpenID associated with the login attempt.</param>
        /// <param name="qrCodeLoginDataType">The QR login flow type.</param>
        public DemoLoginKeyCacheData(string openId, QrCodeLoginDataType qrCodeLoginDataType)
        {
            OpenId = openId;
            QrCodeLoginDataType = qrCodeLoginDataType;
            AddTime = DateTime.Now;
        }
    }

    /// <summary>Provides queue-cache storage for demo login-key entries.</summary>
    public interface IDemoLoginKeyCache : IQueueCache<DemoLoginKeyCacheData>
    {

    }

    [Serializable]
    public class DemoLoginKeyCache : QueueCache<DemoLoginKeyCacheData>, IDemoLoginKeyCache
    {
        private const string cacheKey = "DemoLoginKeyCache";
        private const int timeoutSeconds = 5 * 60;
        /// <summary>
        /// Initializes the demo login-key queue cache.
        /// </summary>
        private DemoLoginKeyCache()
         : base(cacheKey, timeoutSeconds)
        {
        }

        /// <summary>
        /// Creates and asynchronously initializes the singleton-style queue cache instance.
        /// </summary>
        /// <returns>The initialized demo login-key cache.</returns>
        public static async Task<DemoLoginKeyCache> CreateAsync()
        {
            var instance = new DemoLoginKeyCache();

            await instance.InitializeAsync();

            return instance;
        }

        /// <summary>
        /// Connects this cache instance to the shared queue and message collections.
        /// </summary>
        private async Task InitializeAsync()
        {
            var queueCache = await QueueCache<DemoLoginKeyCacheData>.CreateAsync(cacheKey, timeoutSeconds);

            this.MessageQueue = queueCache.MessageQueue;
            this.MessageCollection = queueCache.MessageCollection;
        }

        /// <summary>
        /// Gets a demo login entry and removes it when present, enforcing single-use retrieval.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="removeDataWhenExist">Whether the base cache should remove expired data during lookup.</param>
        /// <returns>The matching entry, or <see langword="null"/> when absent.</returns>
        public override QueueCacheData<DemoLoginKeyCacheData> Get(string key, bool removeDataWhenExist = true)
        {
            var value = base.Get(key,removeDataWhenExist);
            if (value != null)
            {
                base.Remove(key);
            }
            return value;
        }
    }
}
