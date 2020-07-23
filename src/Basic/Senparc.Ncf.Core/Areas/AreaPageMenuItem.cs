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
        public string Url { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }

        public AreaPageMenuItem(string url, string name, string icon)
        {
            Url = url;
            Name = name;
            Icon = icon;
        }
    }
}
