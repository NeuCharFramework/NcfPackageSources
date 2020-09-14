using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Templates.Areas.Admin.Pages.Shared
{
    public partial class _SideMenu : IXncfTemplatePage
    {
        public string RelativeFilePath => $"Areas/Admin/Pages/Shared/_SideMenu.cshtml";

        public string OrgName { get; set; }
        public string XncfName { get; set; }

        public _SideMenu(string orgName, string xncfName)
        {
            OrgName = orgName;
            XncfName = xncfName;
        }
    }
}
