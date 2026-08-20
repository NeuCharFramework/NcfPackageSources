/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SysButtonDto.cs
    文件功能描述：SysButtonDto 相关实现
    
    
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
    public class SysButtonDto : DtoBase
    {

        //public bool IsDeleted { get; set; }

        public string Id { get; set; }

        /// <summary>
        /// 菜单id
        /// </summary>
        [MaxLength(50)]
        public string MenuId { get; set; }

        /// <summary>
        /// 操作名称
        /// </summary>
        [MaxLength(50)]
        //[Required]
        public string ButtonName { get; set; }

        /// <summary>
        /// 操作标识
        /// </summary>
        [MaxLength(50)]
        public string OpearMark { get; set; }

        /// <summary>
        /// 按钮对应的请求地址
        /// </summary>
        [MaxLength(350)]
        public string Url { get; set; }
    }
}
