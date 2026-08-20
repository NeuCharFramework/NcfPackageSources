/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：QrCodeBaseData.cs
    文件功能描述：QrCodeBaseData 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache
{
    /// <summary>
    /// 二维码缓存[暂未使用]
    /// </summary>
    [Serializable]
    public class QrCodeBaseData
    {
        /// <summary>
        /// 即SceneId的字
        /// </summary>
        public string Key { get; set; }
        public int SceneId { get; set; }
        public string Ticket { get; set; }
        public DateTime ExpireTime { get; set; }
        public Guid Guid { get; set; }
        /// <summary>
        /// 验证通过
        /// </summary>
        public bool CheckPassed { get; set; }
        public string UserName { get; set; }

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
