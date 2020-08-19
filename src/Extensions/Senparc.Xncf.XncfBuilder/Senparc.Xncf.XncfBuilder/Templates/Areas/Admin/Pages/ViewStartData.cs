using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuidler.Templates.Areas.Admin.Pages
{
    public partial class ViewStart : IXncfTemplatePage
    {
        public string RelativeFilePath => "Areas/Admin/Pages/Shared/_ViewStart.cshtml";

        public string OrgName { get; set; }
        public string XncfName { get; set; }

        public ViewStart(string orgName, string xncfName)
        {
            OrgName = orgName;
            XncfName = xncfName;
        }
    }
}
