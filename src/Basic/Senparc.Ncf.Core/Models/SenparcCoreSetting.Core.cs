using Senparc.CO2NET;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.MultiTenant;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// Globally adjustable configuration settings
    /// </summary>
    public partial record class SenparcCoreSetting
    {
        /// <summary>
        /// Whether the website has the Debug flag turned on
        /// </summary>
        public bool IsDebug { get; set; }
        /// <summary>
        /// Is it a test station?
        /// </summary>
        public bool IsTestSite { get; set; }

        /// <summary>
        /// Corresponding: In AppData/Database/SenparcConfig.config, the Name of the &lt;SenparcConfig&gt; node of the database connection that needs to be used
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Specify the database type through configuration. Required because of the MultipleDatabaseType enumeration type, please note the case.
        /// </summary>
        public MultipleDatabaseType? DatabaseType { get; set; }

        /// <summary>
        /// cache type
        /// </summary>
        public CacheType CacheType { get; set; }

        #region 多租户

        /// <summary>
        /// Whether to enable multi-tenancy, the default is false
        /// </summary>
        public bool EnableMultiTenant { get; set; }
        /// <summary>
        /// Rules for distinguishing tenants
        /// </summary>
        public TenantRule TenantRule { get; set; } = TenantRule.Default;

        #endregion

        /// <summary>
        /// MemcachedAddresses
        /// </summary>
        public string MemcachedAddresses { get; set; }

        /// <summary>
        /// Request temporary log cache time in cache (minutes), 0 means no caching
        /// </summary>
        public int RequestTempLogCacheMinutes { get; set; }

        /// <summary>
        /// Password encryption strengthening option, this value will not be modified after the first account is generated, otherwise all passwords will become invalid.
        /// </summary>
        public string PasswordSaltToken { get; set; }

        /// <summary>
        ///MCP access token, used for SSE connection authentication
        /// </summary>
        public string McpAccessToken { get; set; }
    }
}
