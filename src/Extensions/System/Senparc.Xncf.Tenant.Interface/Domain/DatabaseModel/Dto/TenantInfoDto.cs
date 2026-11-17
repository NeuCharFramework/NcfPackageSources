/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TenantInfoDto.cs
    File description: TenantInfoDto related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public Guid Guid { get; set; }
        /// <summary>
        /// name
        /// </summary>
        [Required]
        public string Name { get; set; }
        /// <summary>
        /// whether enabled
        /// </summary>
        [Required]
        public bool Enable { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
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
