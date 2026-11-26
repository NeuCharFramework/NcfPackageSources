/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysRole.cs
    File description: SysRole related implementation

    Created by: Senparc - 20200724

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
    /// Represents an administrator role and its enabled/resource-code metadata.
    /// </summary>
    [Table("SysRoles")]
    public class SysRole : EntityBase<string>
    {

        /// <summary>Initializes a role with generated identity and timestamps.</summary>
        public SysRole()
        {
            this.AddTime = DateTime.Now;
            LastUpdateTime = AddTime;
            Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Initializes a role from a DTO.
        /// </summary>
        /// <param name="roleDto">The role values.</param>
        public SysRole(SysRoleDto roleDto) : this()
        {
            RoleName = roleDto.RoleName;
            Enabled = roleDto.Enabled;
            RoleCode = roleDto.RoleCode;
            Remark = roleDto.Remark;
            AdminRemark = roleDto.AdminRemark;
        }

        /// <summary>
        /// Updates editable role values and refreshes the modification timestamp.
        /// </summary>
        /// <param name="roleDto">The replacement role values.</param>
        public void Update(SysRoleDto roleDto)
        {
            LastUpdateTime = DateTime.Now;
            RoleName = roleDto.RoleName;
            RoleCode = roleDto.RoleCode;
            Enabled = roleDto.Enabled;
            Remark = roleDto.Remark;
            AdminRemark = roleDto.AdminRemark;
        }

        /// <summary>
        /// Gets or sets whether the role may be assigned and evaluated.
        /// </summary>
        public bool Enabled { get; set; }

        [MaxLength(150)]
        public new string Id { get; set; }

        /// <summary>
        /// role name
        /// </summary>
        [MaxLength(150)]
        public string RoleName { get; set; }

        /// <summary>
        /// role code
        /// </summary>
        [MaxLength(150)]
        public string RoleCode { get; set; }
    }
}
