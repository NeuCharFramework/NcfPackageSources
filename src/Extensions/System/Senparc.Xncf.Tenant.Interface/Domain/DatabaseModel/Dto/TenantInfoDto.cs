/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：TenantInfoDto.cs
    文件功能描述：TenantInfoDto 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
