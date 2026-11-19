/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysRoleAdminUserInfo.cs
    File description: SysRoleAdminUserInfo related implementation

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
    ///Legacy comment translated to English.
    /// </summary>
    [Table("SysRoleAdminUserInfos")]
    public class SysRoleAdminUserInfo : EntityBase<int>
    {
        public SysRoleAdminUserInfo()
        {
            AddTime = DateTime.Now;
            LastUpdateTime = DateTime.Now;
        }

        public SysRoleAdminUserInfo(int accountId, string roleId, string roleCode) : this()
        {
            AccountId = accountId;
            RoleId = roleId;
            RoleCode = roleCode;
        }

        [MaxLength(150)]
        public string RoleCode { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// roleId
        /// </summary>
        [MaxLength(150)]
        public string RoleId { get; set; }
    }
}
