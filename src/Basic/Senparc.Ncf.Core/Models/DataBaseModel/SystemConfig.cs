/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SystemConfig.cs
    File description: SystemConfig related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.28.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

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
        /// whetherhidemodule management
        /// </summary>
        public bool? HideModuleManager { get; private set; }

        public int NeuCharDeveloperId { get; private set; }

        [MaxLength(100)]
        public string NeuCharAppKey { get; private set; }

        [MaxLength(100)]
        public string NeuCharAppSecret { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
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
        /// update
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
        ///Legacy comment translated to English.
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
