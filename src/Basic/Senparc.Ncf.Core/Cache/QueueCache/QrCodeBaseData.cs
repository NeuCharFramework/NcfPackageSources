using System;

namespace Senparc.Ncf.Core.Cache
{
    /// <summary>
    /// QR code cache [not used yet]
    /// </summary>
    [Serializable]
    public class QrCodeBaseData
    {
        /// <summary>
        /// is the word of SceneId
        /// </summary>
        public string Key { get; set; }
        public int SceneId { get; set; }
        public string Ticket { get; set; }
        public DateTime ExpireTime { get; set; }
        public Guid Guid { get; set; }
        /// <summary>
        ///verification passed
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
