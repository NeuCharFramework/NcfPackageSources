/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SysMenu.cs
    文件功能描述：SysMenu 相关实现
    
    
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
    /// 菜单表
    /// </summary>
    [Table("SysMenus")]
    public class SysMenu : EntityBase<string>
    {

        public SysMenu()
        {
            Id = Guid.NewGuid().ToString();
            AddTime = DateTime.Now;
            this.LastUpdateTime = AddTime;
            ResourceCode = string.Empty;
        }

        public SysMenu(SysMenuDto sysMenuDto) : this()
        {
            LastUpdateTime = DateTime.Now;
            Icon = sysMenuDto.Icon;
            Sort = sysMenuDto.Sort;
            Visible = sysMenuDto.Visible;
            Url = sysMenuDto.Url;
            ParentId = sysMenuDto.ParentId;
            MenuName = sysMenuDto.MenuName;
            IsLocked = sysMenuDto.IsLocked;
            MenuType = sysMenuDto.MenuType;
            ResourceCode = sysMenuDto.ResourceCode ?? string.Empty;
        }

        [MaxLength(150)]
        public new string Id { get; set; }

        [MaxLength(150)]
        [Required]
        public string MenuName { get; set; }

        /// <summary>
        /// 父菜单
        /// </summary>
        [MaxLength(150)]
        public string ParentId { get; set; }

        [MaxLength(500)]
        public string Url { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        [MaxLength(150)]
        public string Icon { get; set; }

        /// <summary>
        /// 是否锁定, 锁定后不能 修改和删除
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        public MenuType MenuType { get; set; }

        /// <summary>
        /// 操作资源
        /// </summary>
        [MaxLength(150)]
        public string ResourceCode { get; set; }

        public void Update(SysMenuDto sysMenuDto)
        {
            this.LastUpdateTime = DateTime.Now;
            Icon = sysMenuDto.Icon;
            Sort = sysMenuDto.Sort;
            Visible = sysMenuDto.Visible;
            Url = sysMenuDto.Url;
            MenuName = sysMenuDto.MenuName;
            MenuType = sysMenuDto.MenuType;
            ResourceCode = sysMenuDto.ResourceCode;
        }
        /// <summary>
        /// 
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool Visible { get; set; }
    }

    public enum MenuType
    {
        无,
        菜单,
        页面,
        按钮
    }
}
