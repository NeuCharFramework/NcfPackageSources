using System;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// 数据库 Dto 基类
    /// </summary>
    public class DtoBase : IDtoBase
    {
        /// <summary>
        /// 是否软删除
        /// </summary>
        public bool Flag { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(150)]
        public string AdminRemark { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(150)]
        public string Remark { get; set; }

        /// <summary>
        /// 添加时间
        /// </summary>
        public DateTime AddTime { get; set; }
        /// <summary>
        /// 上次更新时间
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        /// <summary>
        /// 租户 ID
        /// <para>如果为-1，则本系统不启用多租户</para>
        /// <para>如果为0，则为系统公共数据（特殊情况使用）</para>
        /// </summary>
        public int TenantId { get; set; }
    }
}
