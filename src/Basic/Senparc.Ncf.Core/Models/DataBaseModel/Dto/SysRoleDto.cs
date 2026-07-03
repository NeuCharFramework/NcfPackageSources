/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SysRoleDto.cs
    文件功能描述：SysRoleDto 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    public class SysRoleDto : DtoBase
    {
        [MaxLength(50)]
        public string Id { get; set; }

        /// <summary>
        /// 角色名称
        /// </summary>
        [MaxLength(50)]
        public string RoleName { get; set; }

        /// <summary>
        /// 角色代码
        /// </summary>
        [MaxLength(50)]
        public string RoleCode { get; set; }

        /// <summary>
        /// 启用
        /// </summary>
        public bool Enabled { get; set; }
    }
}
