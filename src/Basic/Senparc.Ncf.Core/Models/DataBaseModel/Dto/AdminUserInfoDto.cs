/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminUserInfoDto.cs
    文件功能描述：AdminUserInfoDto 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// AdminUserInfo 创建和更新
    /// </summary>
    public class AdminUserInfoDto : DtoBase
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public string Note { get; set; }

        public string RealName { get; set; }

        public string Phone { get; set; }
    }

    public class CreateOrUpdate_AdminUserInfoDto : AdminUserInfoDto
    {
        [Required]
        [StringLength(20)]
        new public string UserName { get; set; }
        new public string Password { get; set; }
    }
}
