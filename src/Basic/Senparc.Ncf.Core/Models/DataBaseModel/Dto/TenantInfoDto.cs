using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    public class TenantInfoDto : DtoBase
    {
        /// <summary>
        /// 全局唯一编号（自动分配）
        /// </summary>
        [Required]
        public Guid Guid { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        [Required]
        public string Name { get; set; }
        /// <summary>
        /// 是否启用
        /// </summary>
        [Required]
        public bool Enable { get; set; }
        /// <summary>
        /// 匹配域名、URL、Head的参数
        /// </summary>
        public string TenantKey { get; set; }
    }
}
