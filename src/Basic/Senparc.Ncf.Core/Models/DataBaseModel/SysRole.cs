/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysRole.cs
    File description: SysRole related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
    [Table("SysRoles")]
    public class SysRole : EntityBase<string>
    {

        public SysRole()
        {
            this.AddTime = DateTime.Now;
            LastUpdateTime = AddTime;
            Id = Guid.NewGuid().ToString();
        }

        public SysRole(SysRoleDto roleDto) : this()
        {
            RoleName = roleDto.RoleName;
            Enabled = roleDto.Enabled;
            RoleCode = roleDto.RoleCode;
            Remark = roleDto.Remark;
            AdminRemark = roleDto.AdminRemark;
        }

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
        ///Legacy comment translated to English.
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
