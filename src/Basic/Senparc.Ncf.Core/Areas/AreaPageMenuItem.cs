using Senparc.Ncf.Core.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Areas
{
    /// <summary>
    /// Area 页面菜单项
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
