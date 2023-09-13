using Senparc.CO2NET;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.MultiTenant;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// 全局可调整配置的设置
    /// </summary>
    public partial class SenparcCoreSetting
    {
        /// <summary>
        /// 网站是否开启 Debug 标记
        /// </summary>
        public bool IsDebug { get; set; }
        /// <summary>
        /// 是否是测试站
        /// </summary>
        public bool IsTestSite { get; set; }

        /// <summary>
        /// 对应：AppData/Database/SenparcConfig.config 中，所需要使用的数据库连接的 &lt;SenparcConfig&gt; 节点的 Name
        /// </summary>
        public string DatabaseName { get; set; }
        /// <summary>
        /// 缓存类型
        /// </summary>
        public CacheType CacheType { get; set; }

        #region 多租户

        /// <summary>
        /// 是否启用多租户，默认为 false
        /// </summary>
        public bool EnableMultiTenant { get; set; }
        /// <summary>
        /// 区分租户的规则
        /// </summary>
        public TenantRule TenantRule { get; set; } = TenantRule.Default;

        #endregion

        /// <summary>
        /// MemcachedAddresses
        /// </summary>
        public string MemcachedAddresses { get; set; }

        /// <summary>
        /// 缓存中的请求暂存日志缓存时间（分钟），0 则不缓存
        /// </summary>
        public int RequestTempLogCacheMinutes { get; set; }

        /// <summary>
        /// 密码加密加强选项，此值在首个账号生成后不修改，否则会导致所有密码失效
        /// </summary>
        public string PasswordSaltToken { get; set; }
    }
}
