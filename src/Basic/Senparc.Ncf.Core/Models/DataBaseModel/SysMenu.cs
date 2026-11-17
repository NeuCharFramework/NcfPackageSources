/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: SysMenu.cs

    File description: SysMenu related implementation

    

    

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
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(150)]
        public string ParentId { get; set; }

        [MaxLength(500)]
        public string Url { get; set; }

        /// <summary>
        /// icon
        /// </summary>
        [MaxLength(150)]
        public string Icon { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// type
        /// </summary>
        public MenuType MenuType { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
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
