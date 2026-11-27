/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysMenu.cs
    File description: SysMenu related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

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
    /// Represents an administration menu, page, or button permission node.
    /// </summary>
    [Table("SysMenus")]
    public class SysMenu : EntityBase<string>
    {

        /// <summary>Initializes a menu with generated identity, timestamps, and an empty resource code.</summary>
        public SysMenu()
        {
            Id = Guid.NewGuid().ToString();
            AddTime = DateTime.Now;
            this.LastUpdateTime = AddTime;
            ResourceCode = string.Empty;
        }

        /// <summary>
        /// Initializes a menu from a DTO.
        /// </summary>
        /// <param name="sysMenuDto">The menu values.</param>
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
        /// Gets or sets the parent menu identifier.
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
        /// Gets or sets whether the menu is locked against ordinary editing.
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// type
        /// </summary>
        public MenuType MenuType { get; set; }

        /// <summary>
        /// Gets or sets the resource code used by permission checks.
        /// </summary>
        [MaxLength(150)]
        public string ResourceCode { get; set; }

        /// <summary>
        /// Updates editable menu metadata and refreshes the modification timestamp.
        /// </summary>
        /// <param name="sysMenuDto">The replacement menu values.</param>
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
        /// Gets or sets the display sort order.
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// Gets or sets whether the menu is visible.
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
