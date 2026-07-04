/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SystemConfig.cs
    文件功能描述：SystemConfig 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
        public const int DefaultAdminWebLoginExpireMinutes = 120;
        public const int DefaultBackendJwtExpireMinutes = 150 * 60;

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

        /// <summary>
        /// 后台网页（Cookie）登录过期时长，单位：分钟
        /// </summary>
        public int AdminWebLoginExpireMinutes { get; private set; }

        /// <summary>
        /// 后台 JWT 过期时长，单位：分钟
        /// </summary>
        public int BackendJwtExpireMinutes { get; private set; }

        public SystemConfig(string systemName, string mchId, string mchKey, string tenPayAppId, bool? hideModuleManager, int neuCharDeveloperId, string neuCharAppKey, string neuCharAppSecret, int adminWebLoginExpireMinutes, int backendJwtExpireMinutes)
        {
            SystemName = systemName;
            MchId = mchId;
            MchKey = mchKey;
            TenPayAppId = tenPayAppId;
            HideModuleManager = hideModuleManager;
            NeuCharDeveloperId = neuCharDeveloperId;
            NeuCharAppKey = neuCharAppKey;
            NeuCharAppSecret = neuCharAppSecret;
            AdminWebLoginExpireMinutes = adminWebLoginExpireMinutes > 0 ? adminWebLoginExpireMinutes : DefaultAdminWebLoginExpireMinutes;
            BackendJwtExpireMinutes = backendJwtExpireMinutes > 0 ? backendJwtExpireMinutes : DefaultBackendJwtExpireMinutes;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="systemName"></param>
        /// <param name="mchId"></param>
        /// <param name="mchKey"></param>
        /// <param name="tenPayAppId"></param>
        /// <param name="hideModuleManager"></param>
        public void Update(string systemName, string mchId, string mchKey, string tenPayAppId, bool? hideModuleManager, int adminWebLoginExpireMinutes, int backendJwtExpireMinutes/*, int neuCharDeveloperId, string neuCharAppKey, string neuCharAppSecret*/)
        {
            SystemName = systemName;
            MchId = mchId;
            MchKey = mchKey;
            TenPayAppId = tenPayAppId;
            HideModuleManager = hideModuleManager;
            AdminWebLoginExpireMinutes = adminWebLoginExpireMinutes > 0 ? adminWebLoginExpireMinutes : DefaultAdminWebLoginExpireMinutes;
            BackendJwtExpireMinutes = backendJwtExpireMinutes > 0 ? backendJwtExpireMinutes : DefaultBackendJwtExpireMinutes;
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
