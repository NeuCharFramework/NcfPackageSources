/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: QrCodeBaseData.cs

    File description: QrCodeBaseData related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Serializable]
    public class QrCodeBaseData
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Key { get; set; }
        public int SceneId { get; set; }
        public string Ticket { get; set; }
        public DateTime ExpireTime { get; set; }
        public Guid Guid { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
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
