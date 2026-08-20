/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SysRoleAdminUserInfo.cs
    文件功能描述：SysRoleAdminUserInfo 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
    /// 角色人员表
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
        /// 管理员Id
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// 角色Id
        /// </summary>
        [MaxLength(150)]
        public string RoleId { get; set; }
    }
}
