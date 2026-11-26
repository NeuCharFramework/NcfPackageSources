/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysRoleDto.cs
    File description: SysRoleDto related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>Transfers an administrator role identifier, code, and enablement state.</summary>
    public class SysRoleDto : DtoBase
    {
        [MaxLength(50)]
        public string Id { get; set; }

        /// <summary>
        /// role name
        /// </summary>
        [MaxLength(50)]
        public string RoleName { get; set; }

        /// <summary>
        /// role code
        /// </summary>
        [MaxLength(50)]
        public string RoleCode { get; set; }

        /// <summary>Gets or sets whether the role is enabled.</summary>
        public bool Enabled { get; set; }
    }
}
