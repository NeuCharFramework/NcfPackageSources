/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：QrCodeRegCache.cs
    文件功能描述：QrCodeRegCache 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache
{
    public enum QrCodeRegDataType
    {
        /// <summary>
        /// 注册
        /// </summary>
        Reg,
    }

    [Serializable]
    public class QrCodeRegData
    {
        /// <summary>
        /// 即SceneId的字符串
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
    /// 登录许可缓存（缓存数据：UserId）
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
