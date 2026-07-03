/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SysPermissionDto.cs
    文件功能描述：SysPermissionDto 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    public class SysPermissionDto : DtoBase
    {
        public string RoleId { get; set; }

        public string PermissionId { get; set; }

        public bool IsMenu { get; set; }

        /// <summary>
        /// 角色代码
        /// </summary>
        public string RoleCode { get; set; }

        /// <summary>
        /// 资源（按钮）代码
        /// </summary>
        public string ResourceCode { get; set; }
    }
}
