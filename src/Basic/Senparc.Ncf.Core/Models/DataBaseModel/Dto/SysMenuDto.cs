/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysMenuDto.cs
    File description: SysMenuDto related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>Transfers administration menu, route, visibility, and permission metadata.</summary>
    public class SysMenuDto : DtoBase
    {
      
        /// <summary>Gets or sets the legacy menu flag; use <see cref="MenuType"/> for new code.</summary>
        [Obsolete("使用MenuType字段代替")]
        public bool IsMenu { get; set; }

        [MaxLength(50)]
        public string Id { get; set; }

        [MaxLength(150)]
        [Required]
        public string MenuName { get; set; }

        /// <summary>Gets or sets the parent menu identifier.</summary>
        [MaxLength(50)]
        public string ParentId { get; set; }

        [MaxLength(350)]
        public string Url { get; set; }

        /// <summary>
        /// icon
        /// </summary>
        [MaxLength(50)]
        public string Icon { get; set; }

        /// <summary>Gets or sets the display ordering value.</summary>
        public int Sort { get; set; }

        /// <summary>Gets or sets whether the menu is visible.</summary>
        public bool Visible { get; set; }

        public string ResourceCode { get; set; }

        public bool IsLocked { get; set; }

        /// <summary>
        /// Initializes an empty menu DTO for binding.
        /// </summary>
        public SysMenuDto() { }

        public MenuType MenuType { get; set; }


        /// <summary>
        /// Initializes a menu DTO with its route, hierarchy, display, and permission metadata.
        /// </summary>
        /// <param name="isMenu">The legacy menu flag.</param>
        /// <param name="id">The menu identifier.</param>
        /// <param name="menuName">The display name.</param>
        /// <param name="parentId">The parent menu identifier.</param>
        /// <param name="url">The route URL.</param>
        /// <param name="icon">The icon descriptor.</param>
        /// <param name="sort">The display ordering value.</param>
        /// <param name="visible">Whether the menu is visible.</param>
        /// <param name="resourceCode">The permission/resource code.</param>
        /// <param name="menuType">The menu classification.</param>
        public SysMenuDto(bool isMenu, string id, string menuName, string parentId, string url, string icon, int sort, bool visible, string resourceCode, MenuType menuType = MenuType.菜单)
        {
            IsMenu = isMenu;
            Id = id;
            MenuName = menuName;
            ParentId = parentId;
            Url = url;
            Icon = icon;
            Sort = sort;
            Visible = visible;
            ResourceCode = resourceCode;
            MenuType = menuType;
        }

    }

    /// <summary>Represents one node in the hierarchical administration menu tree.</summary>
    public class SysMenuTreeItemDto
    {
        public string MenuName { get; set; }

        public string Id { get; set; }

        public bool IsMenu { get; set; }

        public IList<SysMenuTreeItemDto> Children { get; set; }
        public string Icon { get; set; }

        public string Url { get; set; }

        /// <summary>
        /// The owning XNCF module's localized menu name, when applicable.
        /// </summary>
        public string ModuleName { get; set; }

        /// <summary>
        /// The owning XNCF module UID, when applicable.
        /// </summary>
        public string ModuleUid { get; set; }
    }
}
