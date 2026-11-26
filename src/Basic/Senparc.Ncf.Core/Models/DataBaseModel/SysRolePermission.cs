/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysRolePermission.cs
    File description: SysRolePermission related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>
    /// Associates a role with a menu or permission resource.
    /// </summary>
    [Table("SysRolePermissions")]
    public class SysRolePermission : EntityBase<int>
    {
        /* Legacy comment translated to English.

         * Legacy comment translated to English.

         */

        /// <summary>Initializes an empty role-permission association.</summary>
        public SysRolePermission()
        {
            AddTime = DateTime.Now;
            LastUpdateTime = DateTime.Now;
        }

        /// <summary>
        /// Initializes a role-permission association from a permission DTO.
        /// </summary>
        /// <param name="item">The permission values to copy.</param>
        public SysRolePermission(SysPermissionDto item) : this()
        {
            RoleId = item.RoleId;
            IsMenu = item.IsMenu;
            PermissionId = item.PermissionId;
            RoleCode = item.RoleCode;
            ResourceCode = item.ResourceCode;
        }

        /// <summary>
        /// role code
        /// </summary>
        [MaxLength(150)]
        public string RoleCode { get; set; }

        /// <summary>
        /// Gets or sets the resource code represented by this association.
        /// </summary>
        [MaxLength(150)]
        public string ResourceCode { get; set; }

        /// <summary>
        /// roleId
        /// </summary>
        [MaxLength(150)]
        public string RoleId { get; set; }

        /// <summary>
        /// Gets or sets whether the associated resource is a menu.
        /// </summary>
        public bool IsMenu { get; set; }

        /// <summary>
        /// Gets or sets the permission identifier.
        /// </summary>
        [MaxLength(150)]
        public string PermissionId { get; set; }
    }
}
