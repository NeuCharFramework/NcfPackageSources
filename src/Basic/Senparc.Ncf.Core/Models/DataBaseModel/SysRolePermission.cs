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
    ///Legacy comment translated to English.
    /// </summary>
    [Table("SysRolePermissions")]
    public class SysRolePermission : EntityBase<int>
    {
        /* Legacy comment translated to English.

         * Legacy comment translated to English.

         */

        public SysRolePermission()
        {
            AddTime = DateTime.Now;
            LastUpdateTime = DateTime.Now;
        }

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
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(150)]
        public string ResourceCode { get; set; }

        /// <summary>
        /// roleId
        /// </summary>
        [MaxLength(150)]
        public string RoleId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsMenu { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(150)]
        public string PermissionId { get; set; }
    }
}
