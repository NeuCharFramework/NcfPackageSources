/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: QrCodeLoginCache.cs
    File description: QrCodeLoginCache related implementation

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
    public enum QrCodeLoginDataType
    {
        /// <summary>Identifies the login flow stored by a QR-code entry.</summary>
        Login,
        /// <summary>
        /// Demo_Hockey_Player1
        /// </summary>
        Demo_Hockey_P1,
        /// <summary>
        /// Demo_Hockey_Player1
        /// </summary>
        Demo_Hockey_P2
    }

    [Serializable]
    public class QrCodeLoginData
    {
        /// <summary>Gets or sets the cache key.</summary>
        public string Key { get; set; }
        /// <summary>Gets or sets the provider scene identifier.</summary>
        public int SceneId { get; set; }
        /// <summary>Gets or sets the provider QR ticket.</summary>
        public string Ticket { get; set; }
        /// <summary>Gets or sets the expiration timestamp.</summary>
        public DateTime ExpireTime { get; set; }
        /// <summary>Gets or sets the login correlation GUID.</summary>
        public Guid LoginGuid { get; set; }
        /// <summary>Gets or sets whether the login was accepted.</summary>
        public bool CheckPassed { get; set; }
        public string UserName { get; set; }
        public QrCodeLoginDataType QrCodeLoginDataType { get; set; }

        /// <summary>
        /// Initializes QR-code login cache data and its provider expiry timestamp.
        /// </summary>
        /// <param name="sceneId">The provider scene identifier.</param>
        /// <param name="expireSeconds">The provider expiry duration in seconds.</param>
        /// <param name="ticket">The provider QR ticket.</param>
        /// <param name="loginGuid">The login correlation GUID.</param>
        /// <param name="qrCodeLoginDataType">The login QR-code type.</param>
        public QrCodeLoginData(int sceneId, int expireSeconds, string ticket, Guid loginGuid, QrCodeLoginDataType qrCodeLoginDataType)
        {
            SceneId = sceneId;
            Key = sceneId.ToString();
            Ticket = ticket;
            ExpireTime = DateTime.Now.AddSeconds(expireSeconds - 5);
            LoginGuid = loginGuid;
            QrCodeLoginDataType = qrCodeLoginDataType;
        }
    }

    /// <summary>Provides queue-cache storage for QR-code login data.</summary>
    public interface IQrCodeLoginCache : IQueueCache<QrCodeLoginData>
    {

    }

    [Serializable]
    public class QrCodeLoginCache : QueueCache<QrCodeLoginData>, IQrCodeLoginCache
    {
        private const string cacheKey = "QrCodeLoginCache";
        private const int timeoutSeconds = -1;
        /// <summary>
        /// Initializes the QR-code login queue cache.
        /// </summary>
        public QrCodeLoginCache()
            : base(cacheKey, timeoutSeconds)
        {

        }

        /// <inheritdoc />
        /// <exception cref="Exception">Always thrown because callers must supply the external key.</exception>
        public override string CreateKey()
        {
            throw new Exception("请在外部生成Key");
        }

        public override QueueCacheData<QrCodeLoginData> Get(string key, bool removeDataWhenExist = true)
        {
            var value = base.Get(key, removeDataWhenExist);
            //if (value != null)
            //{
            //Legacy comment translated to English.
            //}
            return value;
        }
    }
}
