using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    ///Multi-tenant interface
    /// </summary>
    public interface IMultiTenancy
    {
        /// <summary>
        ///TenantId
        /// </summary>
        public int TenantId { get; set; }
    }
}
