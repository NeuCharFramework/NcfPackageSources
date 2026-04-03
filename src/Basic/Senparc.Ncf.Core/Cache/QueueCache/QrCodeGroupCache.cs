using System;

namespace Senparc.Ncf.Core.Cache.QueueCache
{
    public enum QrCodeGroupDataType
    {
        /// <summary>
        /// sect
        /// </summary>
        Group,
    }

    [Serializable]
    public class QrCodeGroupData
    {
        /// <summary>
        /// is the string of SceneId
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
    /// Martial arts license cache (cache data: GroupId)
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
            throw new Exception("Please generate Key externally");
        }

        public override QueueCacheData<QrCodeGroupData> Get(string key, bool removeDataWhenExist = true)
        {
            var value = base.Get(key, removeDataWhenExist);
            return value;
        }
    }
}