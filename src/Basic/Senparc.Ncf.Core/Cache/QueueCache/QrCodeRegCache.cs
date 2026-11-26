/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: QrCodeRegCache.cs
    File description: QrCodeRegCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache
{
    public enum QrCodeRegDataType
    {
        /// <summary>Identifies the QR-code registration data category.</summary>
        Reg,
    }

    [Serializable]
    public class QrCodeRegData
    {
        /// <summary>Gets or sets the externally supplied cache key.</summary>
        public string Key { get; set; }
        /// <summary>Gets or sets the provider scene identifier.</summary>
        public int SceneId { get; set; }
        /// <summary>Gets or sets the provider QR ticket.</summary>
        public string Ticket { get; set; }
        /// <summary>Gets or sets the expiration timestamp.</summary>
        public DateTime ExpireTime { get; set; }
        /// <summary>Gets or sets the registration correlation GUID.</summary>
        public Guid RegGuid { get; set; }
        /// <summary>Gets or sets the verification code.</summary>
        public string Code { get; set; }
        /// <summary>Gets or sets the Weixin OpenID.</summary>
        public string OpenId { get; set; }
        public QrCodeRegDataType QrCodeRegDataType { get; set; }

        /// <summary>
        /// Initializes QR-code registration cache data and its provider expiry timestamp.
        /// </summary>
        /// <param name="sceneId">The provider scene identifier.</param>
        /// <param name="expireSeconds">The provider expiry duration in seconds.</param>
        /// <param name="ticket">The provider QR ticket.</param>
        /// <param name="regGuid">The registration correlation GUID.</param>
        /// <param name="qrCodeRegDataType">The registration QR-code type.</param>
        public QrCodeRegData(int sceneId, int expireSeconds, string ticket, Guid regGuid, QrCodeRegDataType qrCodeRegDataType)
        {
            SceneId = sceneId;
            Key = sceneId.ToString();
            Ticket = ticket;
            ExpireTime = DateTime.Now.AddSeconds(expireSeconds - 5);
            RegGuid = regGuid;
            QrCodeRegDataType = qrCodeRegDataType;
        }
    }

    /// <summary>Provides queue-cache storage for QR-code registration data.</summary>
    public interface IQrCodeRegCache : IQueueCache<QrCodeRegData>
    {

    }

    [Serializable]
    public class QrCodeRegCache : QueueCache<QrCodeRegData>, IQrCodeRegCache
    {
        private const string cacheKey = "QrCodeRegCache";
        private const int timeoutSeconds = -1;
        /// <summary>
        /// Initializes the QR-code registration queue cache.
        /// </summary>
        public QrCodeRegCache()
            : base(cacheKey, timeoutSeconds)
        {

        }

        /// <inheritdoc />
        /// <exception cref="Exception">Always thrown because callers must provide the external key.</exception>
        public override string CreateKey()
        {
            throw new Exception("请在外部生成Key");
        }

        public override QueueCacheData<QrCodeRegData> Get(string key, bool removeDataWhenExist = true)
        {
            var value = base.Get(key, removeDataWhenExist);
            return value;
        }
    }
}
