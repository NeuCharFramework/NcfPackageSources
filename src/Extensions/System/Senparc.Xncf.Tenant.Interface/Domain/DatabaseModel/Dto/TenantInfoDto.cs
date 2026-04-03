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
        /// Globally unique number (automatically assigned)
        /// </summary>
        [Required]
        public Guid Guid { get; set; }
        /// <summary>
        ///name
        /// </summary>
        [Required]
        public string Name { get; set; }
        /// <summary>
        /// Whether to enable
        /// </summary>
        [Required]
        public bool Enable { get; set; }
        /// <summary>
        /// Match parameters of domain name, URL, and Head
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
