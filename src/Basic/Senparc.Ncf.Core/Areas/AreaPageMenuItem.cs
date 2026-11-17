/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AreaPageMenuItem.cs
    File description: AreaPageMenuItem related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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

        public AreaPageMenuItem(string url, string name, string icon)
        {
            Url = url;
            Name = name;
            Icon = icon;
        }

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
