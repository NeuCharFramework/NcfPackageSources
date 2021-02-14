using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Ncf.Core.Models
{
    [Serializable]
    public partial class SystemConfig : EntityBase<int>
    {
        [Required]
        [Column(TypeName = "nvarchar(100)")]
        public string SystemName { get; private set; }

        [Column(TypeName = "varchar(100)")]
        public string MchId { get; private set; }

        [Column(TypeName = "varchar(300)")]
        public string MchKey { get; private set; }

        [Column(TypeName = "varchar(100)")]
        public string TenPayAppId { get; private set; }

        /// <summary>
        /// 是否隐藏模块管理
        /// </summary>
        public bool? HideModuleManager { get; private set; }

        public SystemConfig(string systemName, string mchId, string mchKey, string tenPayAppId, bool? hideModuleManager)
        {
            SystemName = systemName;
            MchId = mchId;
            MchKey = mchKey;
            TenPayAppId = tenPayAppId;
            HideModuleManager = hideModuleManager;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="systemName"></param>
        /// <param name="mchId"></param>
        /// <param name="mchKey"></param>
        /// <param name="tenPayAppId"></param>
        /// <param name="hideModuleManager"></param>
        public void Update(string systemName, string mchId, string mchKey, string tenPayAppId, bool? hideModuleManager)
        {
            SystemName = systemName;
            MchId = mchId;
            MchKey = mchKey;
            TenPayAppId = tenPayAppId;
            HideModuleManager = hideModuleManager;
        }

    }
}
