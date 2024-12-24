using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Ncf.Core.Models
{
    [Serializable]
    [Table("SystemConfigs")]
    public partial class SystemConfig : EntityBase<int>
    {
        [Required]
        [MaxLength(100)]
        public string SystemName { get; private set; }

        [MaxLength(100)]
        public string MchId { get; private set; }

        [MaxLength(300)]
        public string MchKey { get; private set; }

        [MaxLength(100)]
        public string TenPayAppId { get; private set; }

        /// <summary>
        /// 是否隐藏模块管理
        /// </summary>
        public bool? HideModuleManager { get; private set; }

        public int NeuCharDeveloperId { get; private set; }

        [MaxLength(100)]
        public string NeuCharAppKey { get; private set; }

        [MaxLength(100)]
        public string NeuCharAppSecret { get; private set; }

        public SystemConfig(string systemName, string mchId, string mchKey, string tenPayAppId, bool? hideModuleManager, int neuCharDeveloperId, string neuCharAppKey, string neuCharAppSecret)
        {
            SystemName = systemName;
            MchId = mchId;
            MchKey = mchKey;
            TenPayAppId = tenPayAppId;
            HideModuleManager = hideModuleManager;
            NeuCharDeveloperId = neuCharDeveloperId;
            NeuCharAppKey = neuCharAppKey;
            NeuCharAppSecret = neuCharAppSecret;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="systemName"></param>
        /// <param name="mchId"></param>
        /// <param name="mchKey"></param>
        /// <param name="tenPayAppId"></param>
        /// <param name="hideModuleManager"></param>
        public void Update(string systemName, string mchId, string mchKey, string tenPayAppId, bool? hideModuleManager/*, int neuCharDeveloperId, string neuCharAppKey, string neuCharAppSecret*/)
        {
            SystemName = systemName;
            MchId = mchId;
            MchKey = mchKey;
            TenPayAppId = tenPayAppId;
            HideModuleManager = hideModuleManager;
            //NeuCharDeveloperId = neuCharDeveloperId;
            //NeuCharAppKey = neuCharAppKey;
            //NeuCharAppSecret = neuCharAppSecret;
        }

        public void UpdateNeuCharAccount(int developerId, string appKey, string appSecret)
        {
            this.NeuCharDeveloperId = developerId;
            this.NeuCharAppKey = appKey;
            this.NeuCharAppSecret = appSecret;
        }
    }
}
