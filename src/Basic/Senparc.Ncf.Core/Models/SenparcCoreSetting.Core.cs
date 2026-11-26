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
    /// Contains host-level NCF runtime, database, cache, tenant, and security settings.
    /// </summary>
    public partial record class SenparcCoreSetting
    {
        /// <summary>Gets or sets whether debug behavior is enabled.</summary>
        public bool IsDebug { get; set; }
        /// <summary>Gets or sets whether the host is marked as a test site.</summary>
        public bool IsTestSite { get; set; }

        /// <summary>Gets or sets the logical database configuration name.</summary>
        public string DatabaseName { get; set; }

        /// <summary>Gets or sets the selected database provider.</summary>
        public MultipleDatabaseType? DatabaseType { get; set; }

        /// <summary>Gets or sets the cache provider type.</summary>
        public CacheType CacheType { get; set; }

        #region Legacy section

        /// <summary>
        /// whether enabledmulti-tenancy，defaultfor false
        /// </summary>
        public bool EnableMultiTenant { get; set; }
        /// <summary>Gets or sets how tenants are resolved from incoming requests.</summary>
        public TenantRule TenantRule { get; set; } = TenantRule.Default;

        #endregion

        /// <summary>
        /// MemcachedAddresses
        /// </summary>
        public string MemcachedAddresses { get; set; }

        /// <summary>Gets or sets the request-temporary-log cache lifetime in minutes.</summary>
        public int RequestTempLogCacheMinutes { get; set; }

        /// <summary>Gets or sets the password-salt token used by core security helpers.</summary>
        public string PasswordSaltToken { get; set; }

        /// <summary>Gets or sets the MCP access token used by protected MCP endpoints.</summary>
        public string McpAccessToken { get; set; }
    }
}
