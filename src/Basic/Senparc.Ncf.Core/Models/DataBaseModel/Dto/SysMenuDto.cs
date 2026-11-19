/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysMenuDto.cs
    File description: SysMenuDto related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    public class SysMenuDto : DtoBase
    {
      
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Obsolete("使用MenuType字段代替")]
        public bool IsMenu { get; set; }

        [MaxLength(50)]
        public string Id { get; set; }

        [MaxLength(150)]
        [Required]
        public string MenuName { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(50)]
        public string ParentId { get; set; }

        [MaxLength(350)]
        public string Url { get; set; }

        /// <summary>
        /// icon
        /// </summary>
        [MaxLength(50)]
        public string Icon { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool Visible { get; set; }

        public string ResourceCode { get; set; }

        public bool IsLocked { get; set; }

        public SysMenuDto() { }

        public MenuType MenuType { get; set; }


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

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
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
