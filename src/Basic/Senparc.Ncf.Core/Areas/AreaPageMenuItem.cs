/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AreaPageMenuItem.cs
    File description: AreaPageMenuItem related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Areas
{
    /// <summary>
    /// Area page menu item
    /// </summary>
    public class AreaPageMenuItem
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string ParentId { get; set; }
        public MenuType MenuType { get; set; }

        /// <summary>
        /// Initializes a top-level area menu item.
        /// </summary>
        /// <param name="url">The target URL.</param>
        /// <param name="name">The display name.</param>
        /// <param name="icon">The CSS/icon class.</param>
        public AreaPageMenuItem(string url, string name, string icon)
        {
            Url = url;
            Name = name;
            Icon = icon;
        }

        /// <summary>
        /// Initializes a menu item with identity, hierarchy, URL, icon, and menu type metadata.
        /// </summary>
        /// <param name="id">The menu identifier.</param>
        /// <param name="url">The target URL.</param>
        /// <param name="name">The display name.</param>
        /// <param name="icon">The CSS/icon class.</param>
        /// <param name="parentId">The parent menu identifier.</param>
        /// <param name="menuType">The menu type.</param>
        public AreaPageMenuItem(string id,string url, string name, string icon,string parentId,MenuType menuType)
        {
            Id = id;
            Url = url;
            Name = name;
            Icon = icon;
            ParentId = parentId;
            MenuType = menuType;
        }
    }
}
