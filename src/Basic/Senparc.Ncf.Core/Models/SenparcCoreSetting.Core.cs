/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenparcCoreSetting.Core.cs
    File description: SenparcCoreSetting.Core related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.MultiTenant;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public partial record class SenparcCoreSetting
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsDebug { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsTestSite { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public MultipleDatabaseType? DatabaseType { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public CacheType CacheType { get; set; }

        #region Legacy section

        /// <summary>
        /// whether enabledmulti-tenancy，defaultfor false
        /// </summary>
        public bool EnableMultiTenant { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public TenantRule TenantRule { get; set; } = TenantRule.Default;

        #endregion

        /// <summary>
        /// MemcachedAddresses
        /// </summary>
        public string MemcachedAddresses { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int RequestTempLogCacheMinutes { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string PasswordSaltToken { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string McpAccessToken { get; set; }
    }
}
