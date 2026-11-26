/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SysMenu_Response.cs
    File description: SysMenu_Response related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.PL
{
    public class SysMenu_Response
    {

    }

    /// <summary>
    /// Contains the identifier returned after a menu create or update operation.
    /// </summary>
    public class SysMenu_CreateOrUpdateResponse
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Id { get; set; }
    }

    /// <summary>
    /// Contains the hierarchical menu entries returned by the menu-tree endpoint.
    /// </summary>
    public class SysMenu_MenuTreeResponse
    {
        /// <summary>
        /// Gets or sets the hierarchical menu entries.
        /// </summary>
        public IEnumerable<Ncf.Core.Models.DataBaseModel.SysMenuTreeItemDto> Items { get; set; }
    }

    /// <summary>
    /// Contains the flat menu entries returned by the menu-list endpoint.
    /// </summary>
    public class SysMenu_MenusResponse
    {
        /// <summary>
        /// Gets or sets the menu DTO collection.
        /// </summary>
        public IEnumerable<Ncf.Core.Models.DataBaseModel.SysMenuDto> Items { get; set; }
    }

    /// <summary>
    /// Contains one menu DTO returned by the menu-detail endpoint.
    /// </summary>
    public class SysMenu_GetMenuResponse
    {
        /// <summary>
        /// Gets or sets the requested menu DTO.
        /// </summary>
        public Ncf.Core.Models.DataBaseModel.SysMenuDto Item { get; set; }
    }
}
