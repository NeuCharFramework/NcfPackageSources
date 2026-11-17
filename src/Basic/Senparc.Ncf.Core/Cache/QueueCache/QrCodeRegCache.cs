/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: QrCodeRegCache.cs
    File description: QrCodeRegCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache
{
    public enum QrCodeRegDataType
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Reg,
    }

    [Serializable]
    public class QrCodeRegData
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Key { get; set; }
        public int SceneId { get; set; }
        public string Ticket { get; set; }
        public DateTime ExpireTime { get; set; }
        public Guid RegGuid { get; set; }
        public string Code { get; set; }
        public string OpenId { get; set; }
        public QrCodeRegDataType QrCodeRegDataType { get; set; }

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

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public interface IQrCodeRegCache : IQueueCache<QrCodeRegData>
    {

    }

    [Serializable]
    public class QrCodeRegCache : QueueCache<QrCodeRegData>, IQrCodeRegCache
    {
        private const string cacheKey = "QrCodeRegCache";
        private const int timeoutSeconds = -1;
        public QrCodeRegCache()
            : base(cacheKey, timeoutSeconds)
        {

        }

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
