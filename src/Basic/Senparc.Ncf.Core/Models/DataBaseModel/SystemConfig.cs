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
    /// <summary>
    /// Stores core system identity, payment integration, NeuChar credentials, and footer settings.
    /// </summary>
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

        /// <summary>Gets or sets normalized footer content shown by the host.</summary>
        [Required]
        [MaxLength(2000)]
        public string FooterContent { get; private set; }

        /// <summary>
        /// Initializes system configuration, preserving the supplied payment, module, NeuChar,
        /// and footer settings.
        /// </summary>
        /// <param name="systemName">The display system name.</param>
        /// <param name="mchId">The payment merchant identifier.</param>
        /// <param name="mchKey">The payment merchant key.</param>
        /// <param name="tenPayAppId">The payment application identifier.</param>
        /// <param name="hideModuleManager">Whether the module manager should be hidden.</param>
        /// <param name="neuCharDeveloperId">The NeuChar developer identifier.</param>
        /// <param name="neuCharAppKey">The NeuChar application key.</param>
        /// <param name="neuCharAppSecret">The NeuChar application secret.</param>
        /// <param name="footerContent">Optional footer content; blank values use the default footer.</param>
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
        /// Updates editable system identity, payment and module-visibility settings.
        /// </summary>
        /// <param name="systemName">The display system name.</param>
        /// <param name="mchId">The payment merchant identifier.</param>
        /// <param name="mchKey">The payment merchant key.</param>
        /// <param name="tenPayAppId">The payment application identifier.</param>
        /// <param name="hideModuleManager">Whether the module manager should be hidden.</param>
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

        /// <summary>
        /// Replaces the stored NeuChar developer credentials.
        /// </summary>
        /// <param name="developerId">The developer identifier.</param>
        /// <param name="appKey">The application key.</param>
        /// <param name="appSecret">The application secret.</param>
        public void UpdateNeuCharAccount(int developerId, string appKey, string appSecret)
        {
            this.NeuCharDeveloperId = developerId;
            this.NeuCharAppKey = appKey;
            this.NeuCharAppSecret = appSecret;
        }

        /// <summary>
        /// Normalizes and stores footer content, using the default footer when blank.
        /// </summary>
        /// <param name="footerContent">The footer content.</param>
        public void UpdateFooterContent(string footerContent)
        {
            FooterContent = NormalizeFooterContent(footerContent);
        }

        /// <summary>
        /// Creates the default copyright footer for the supplied or current year.
        /// </summary>
        /// <param name="now">Optional timestamp used to determine the year.</param>
        /// <returns>The default copyright footer.</returns>
        public static string CreateDefaultFooterContent(DateTime? now = null)
        {
            return $"© {(now ?? DateTime.Now).Year} Senparc";
        }

        /// <summary>
        /// Normalizes footer content for storage, applying the default footer when input is blank.
        /// </summary>
        /// <param name="footerContent">The requested footer content.</param>
        /// <returns>The normalized footer content.</returns>
        private static string NormalizeFooterContent(string footerContent)
        {
            return string.IsNullOrWhiteSpace(footerContent)
                ? CreateDefaultFooterContent()
                : footerContent.Trim();
        }
    }
}
