/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AreaPageMenuItem.cs
    文件功能描述：AreaPageMenuItem 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
