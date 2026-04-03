using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.MultiTenant
{
    /// <summary>
    ///tenant rules
    /// </summary>
    public enum TenantRule
    {
        /// <summary>
        /// Distinguish by domain name
        /// </summary>
        DomainName = 0,
        /// <summary>
        /// Request Header information in the request
        /// </summary>
        RequestHeader = 1,
        /// <summary>
        /// Distinguish by tenant name entered at login
        /// </summary>
        LoginInput = 2,
        /// <summary>
        /// default
        /// </summary>
        Default = LoginInput
    }
}
