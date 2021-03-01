using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.MultiTenant
{
    /// <summary>
    /// 租户规则
    /// </summary>
    public enum TenantRule
    {
        /// <summary>
        /// 以域名区分
        /// </summary>
        DomainName = 0,
        /// <summary>
        /// Request 请求中的 Header 信息
        /// </summary>
        RequestHeader = 1,
        /// <summary>
        /// 默认
        /// </summary>
        Default = DomainName
    }
}
