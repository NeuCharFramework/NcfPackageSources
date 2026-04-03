using System;

namespace Senparc.Ncf.Core.Cache
{
    public enum QrCodeLoginDataType
    {
        /// <summary>
        ///login
        /// </summary>
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
        /// <summary>
        /// is the string of SceneId
        /// </summary>
        public string Key { get; set; }
        public int SceneId { get; set; }
        public string Ticket { get; set; }
        public DateTime ExpireTime { get; set; }
        public Guid LoginGuid { get; set; }
        /// <summary>
        ///verification passed
        /// </summary>
        public bool CheckPassed { get; set; }
        public string UserName { get; set; }
        public QrCodeLoginDataType QrCodeLoginDataType { get; set; }

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

    /// <summary>
    /// Login permission cache (cache data: UserId)
    /// </summary>
    public interface IQrCodeLoginCache : IQueueCache<QrCodeLoginData>
    {

    }

    [Serializable]
    public class QrCodeLoginCache : QueueCache<QrCodeLoginData>, IQrCodeLoginCache
    {
        private const string cacheKey = "QrCodeLoginCache";
        private const int timeoutSeconds = -1;
        public QrCodeLoginCache()
            : base(cacheKey, timeoutSeconds)
        {

        }

        public override string CreateKey()
        {
            throw new Exception("Please generate Key externally");
        }

        public override QueueCacheData<QrCodeLoginData> Get(string key, bool removeDataWhenExist = true)
        {
            var value = base.Get(key, removeDataWhenExist);
            //if (value != null)
            //{
            //    base.Remove(key);  // One-time use
            //}
            return value;
        }
    }
}
