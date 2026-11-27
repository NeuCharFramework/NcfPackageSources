/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TenantInfoDto.cs
    File description: TenantInfoDto related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.10.13-preview13 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.10.13.2-preview14 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Xncf.Tenant.Domain.DataBaseModel
{
    /// <summary>Transfers tenant identity and enablement settings between administration layers.</summary>
    public class TenantInfoDto : DtoBase
    {
        public int Id { get; set; }
        /// <summary>Gets or sets the stable tenant GUID.</summary>
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
        /// <summary>Gets or sets the normalized tenant lookup key.</summary>
        public string TenantKey { get; set; }
    }

    /// <summary>Contains tenant values used to create or update a tenant record.</summary>
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
