/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: QrCodeLoginCache.cs

    File description: QrCodeLoginCache related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache
{
    public enum QrCodeLoginDataType
    {
        /// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        public string Key { get; set; }
        public int SceneId { get; set; }
        public string Ticket { get; set; }
        public DateTime ExpireTime { get; set; }
        public Guid LoginGuid { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
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
    ///Legacy comment translated to English.
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
