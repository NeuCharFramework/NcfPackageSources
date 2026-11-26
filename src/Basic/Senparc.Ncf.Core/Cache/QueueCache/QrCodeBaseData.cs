/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: QrCodeBaseData.cs
    File description: QrCodeBaseData related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache
{
    /// <summary>Stores the shared state of a QR-code based verification flow.</summary>
    [Serializable]
    public class QrCodeBaseData
    {
        /// <summary>Gets or sets the cache key, normally derived from the scene identifier.</summary>
        public string Key { get; set; }
        /// <summary>Gets or sets the provider scene identifier.</summary>
        public int SceneId { get; set; }
        /// <summary>Gets or sets the provider QR ticket.</summary>
        public string Ticket { get; set; }
        /// <summary>Gets or sets the expiration timestamp.</summary>
        public DateTime ExpireTime { get; set; }
        /// <summary>Gets or sets the application correlation GUID.</summary>
        public Guid Guid { get; set; }
        /// <summary>Gets or sets whether the code has been accepted.</summary>
        public bool CheckPassed { get; set; }
        public string UserName { get; set; }

        /// <summary>
        /// Initializes QR-code cache data from the provider scene, ticket, expiry, and correlation ID.
        /// </summary>
        /// <param name="sceneId">The provider scene identifier.</param>
        /// <param name="expireSeconds">The provider expiry duration in seconds.</param>
        /// <param name="ticket">The provider QR ticket.</param>
        /// <param name="guid">The application correlation GUID.</param>
        public QrCodeBaseData(int sceneId, int expireSeconds, string ticket, Guid guid)
        {
            SceneId = sceneId;
            Key = sceneId.ToString();
            Ticket = ticket;
            ExpireTime = DateTime.Now.AddSeconds(expireSeconds - 5);
            Guid = guid;
        }
    }
}
