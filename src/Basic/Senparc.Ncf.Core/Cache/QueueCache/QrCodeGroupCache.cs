/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: QrCodeGroupCache.cs
    File description: QrCodeGroupCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache.QueueCache
{
    public enum QrCodeGroupDataType
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Group,
    }

    [Serializable]
    public class QrCodeGroupData
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// GroupId
        /// </summary>
        public int SceneId { get; set; }
        public string Ticket { get; set; }
        public DateTime ExpireTime { get; set; }


        public QrCodeGroupDataType QrCodeGroupDataType { get; set; }

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

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public interface IQrCodeGroupCache : IQueueCache<QrCodeGroupData>
    {
    }

    [Serializable]
    public class QrCodeGroupCache : QueueCache<QrCodeGroupData>, IQrCodeGroupCache
    {
        private const string cacheKey = "QrCodeGroupCache";
        private const int timeoutSeconds = 604800;

        public QrCodeGroupCache()
            : base(cacheKey, timeoutSeconds)
        {
        }

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