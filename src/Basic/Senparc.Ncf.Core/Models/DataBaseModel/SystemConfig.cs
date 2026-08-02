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
        /// 站点与管理后台共用的底部版权内容。允许的 HTML 会在渲染时由
        /// <see cref="Utility.FooterContentSanitizer"/> 限制为安全链接。
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string FooterContent { get; private set; }

        public SystemConfig(string systemName, string mchId, string mchKey, string tenPayAppId, bool? hideModuleManager, int neuCharDeveloperId, string neuCharAppKey, string neuCharAppSecret, string footerContent = null)
        {
            SystemName = systemName;
            MchId = mchId;
            MchKey = mchKey;
            TenPayAppId = tenPayAppId;
            HideModuleManager = hideModuleManager;
            NeuCharDeveloperId = neuCharDeveloperId;
            NeuCharAppKey = neuCharAppKey;
            NeuCharAppSecret = neuCharAppSecret;
            FooterContent = NormalizeFooterContent(footerContent);
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

        /// <summary>
        /// 更新站点与管理后台共用的底部版权内容。
        /// </summary>
        public void UpdateFooterContent(string footerContent)
        {
            FooterContent = NormalizeFooterContent(footerContent);
        }

        public static string CreateDefaultFooterContent(DateTime? now = null)
        {
            return $"© {(now ?? DateTime.Now).Year} Senparc";
        }

        private static string NormalizeFooterContent(string footerContent)
        {
            return string.IsNullOrWhiteSpace(footerContent)
                ? CreateDefaultFooterContent()
                : footerContent.Trim();
        }
    }
}
