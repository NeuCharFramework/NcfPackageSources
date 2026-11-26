/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysPermissionDto.cs
    File description: SysPermissionDto related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>Transfers a role-to-menu or role-to-resource permission mapping.</summary>
    public class SysPermissionDto : DtoBase
    {
        /// <summary>Gets or sets the role identifier.</summary>
        public string RoleId { get; set; }

        /// <summary>Gets or sets the menu or permission identifier.</summary>
        public string PermissionId { get; set; }

        /// <summary>Gets or sets whether the permission represents a menu.</summary>
        public bool IsMenu { get; set; }

        /// <summary>
        /// role code
        /// </summary>
        public string RoleCode { get; set; }

        /// <summary>Gets or sets the resource code for button/API permission checks.</summary>
        public string ResourceCode { get; set; }
    }
}
