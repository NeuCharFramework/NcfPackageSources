using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// 多租户接口
    /// </summary>
    public interface IMultiTenancy
    {
        /// <summary>
        /// 租户Id
        /// </summary>
        public int TenantId { get; set; }
    }
}
