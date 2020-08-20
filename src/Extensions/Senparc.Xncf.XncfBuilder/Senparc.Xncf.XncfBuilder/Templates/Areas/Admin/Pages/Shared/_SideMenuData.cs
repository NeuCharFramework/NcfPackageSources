using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuidler.Templates.Areas.Admin.Pages.Shared
{
    public partial class _SideMenu : IXncfTemplatePage
    {
        public string RelativeFilePath => $"_SideMenu.cshtml";

        public string OrgName { get; set; }
        public string XncfName { get; set; }
    }
}
