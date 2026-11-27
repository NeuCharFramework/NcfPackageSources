/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: QrCodeGroupCache.cs
    File description: QrCodeGroupCache related implementation

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
    public enum QrCodeGroupDataType
    {
        /// <summary>Identifies the QR-code group flow represented by the cached data.</summary>
        Group,
    }

    [Serializable]
    public class QrCodeGroupData
    {
        /// <summary>Gets or sets the cache key.</summary>
        public string Key { get; set; }
        /// <summary>
        /// GroupId
        /// </summary>
        /// <summary>Gets or sets the provider scene identifier.</summary>
        public int SceneId { get; set; }
        /// <summary>Gets or sets the provider QR ticket.</summary>
        public string Ticket { get; set; }
        /// <summary>Gets or sets the expiration timestamp.</summary>
        public DateTime ExpireTime { get; set; }


        public QrCodeGroupDataType QrCodeGroupDataType { get; set; }

        /// <summary>
        /// Initializes QR-code group cache data and its provider expiry timestamp.
        /// </summary>
        /// <param name="sceneId">The provider scene identifier.</param>
        /// <param name="expireSeconds">The provider expiry duration in seconds.</param>
        /// <param name="ticket">The provider QR ticket.</param>
        /// <param name="qrCodeGroupDataType">The QR-code group data type.</param>
        public QrCodeGroupData(int sceneId, int expireSeconds, string ticket,
            QrCodeGroupDataType qrCodeGroupDataType)
        {
            SceneId = sceneId;
            Key = sceneId.ToString();
            Ticket = ticket;
            ExpireTime = DateTime.Now.AddSeconds(expireSeconds - 5);
            //UpDataTime = DateTime.Now;
            //AddTime = DateTime.Now;
            QrCodeGroupDataType = qrCodeGroupDataType;
        }
    }

    /// <summary>Provides queue-cache storage for QR-code group data.</summary>
    public interface IQrCodeGroupCache : IQueueCache<QrCodeGroupData>
    {
    }

    [Serializable]
    public class QrCodeGroupCache : QueueCache<QrCodeGroupData>, IQrCodeGroupCache
    {
        private const string cacheKey = "QrCodeGroupCache";
        private const int timeoutSeconds = 604800;

        /// <summary>
        /// Initializes the QR-code group queue cache with a seven-day timeout.
        /// </summary>
        public QrCodeGroupCache()
            : base(cacheKey, timeoutSeconds)
        {
        }

        /// <inheritdoc />
        /// <exception cref="Exception">Always thrown because callers must supply the external key.</exception>
        public override string CreateKey()
        {
            throw new Exception("请在外部生成Key");
        }

        public override QueueCacheData<QrCodeGroupData> Get(string key, bool removeDataWhenExist = true)
        {
            var value = base.Get(key, removeDataWhenExist);
            return value;
        }
    }
}
