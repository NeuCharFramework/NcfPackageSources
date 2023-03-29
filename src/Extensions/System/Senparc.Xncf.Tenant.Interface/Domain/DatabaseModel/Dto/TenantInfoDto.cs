using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Xncf.Tenant.Domain.DataBaseModel
{
    public class TenantInfoDto : DtoBase
    {
        public int Id { get; set; }
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

    public class CreateOrUpdate_TenantInfoDto : TenantInfoDto
    {
        public CreateOrUpdate_TenantInfoDto(int id, string name, string tenantKey, bool enable, string adminRemark)
        {
            Id = id;
            Name = name;
            TenantKey = tenantKey.ToUpper();
            Enable = enable;
            AdminRemark = adminRemark;
        }
    }
}
