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
    }
}
