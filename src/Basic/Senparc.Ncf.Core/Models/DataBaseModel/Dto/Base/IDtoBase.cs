using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// 所有 DTO 接口或类的基类
    /// </summary>
    public interface IDtoBase
    {
        /// <summary>
        /// 是否软删除
        /// </summary>
        bool Flag { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(150)]
        string AdminRemark { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(150)]
        string Remark { get; set; }

        /// <summary>
        /// 添加时间
        /// </summary>
        DateTime AddTime { get; set; }
        /// <summary>
        /// 上次更新时间
        /// </summary>
        DateTime LastUpdateTime { get; set; }

        /// <summary>
        /// 租户 ID
        /// <para>如果为-1，则本系统不启用多租户</para>
        /// <para>如果为0，则为系统公共数据（特殊情况使用）</para>
        /// </summary>
        public int TenantId { get; set; }
    }
}
