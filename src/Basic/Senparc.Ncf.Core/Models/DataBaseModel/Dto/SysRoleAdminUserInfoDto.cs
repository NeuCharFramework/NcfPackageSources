/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SysRoleAdminUserInfoDto.cs
    文件功能描述：SysRoleAdminUserInfoDto 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    public class SysRoleAdminUserInfoDto : DtoBase
    {
        /// <summary>
        /// 角色名称
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        /// 角色编号
        /// </summary>
        public string RoleId { get; set; }

        /// <summary>
        /// 用户编号
        /// </summary>
        public int AdminAccountId { get; set; }

        /// <summary>
        /// 当前用户是否有此角色
        /// </summary>
        public bool HasRole { get; set; }
    }
}
