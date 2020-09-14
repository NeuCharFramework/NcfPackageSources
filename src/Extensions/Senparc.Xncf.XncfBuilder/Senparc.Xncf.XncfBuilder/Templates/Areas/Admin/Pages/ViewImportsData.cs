using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Templates.Areas.Admin.Pages
{
    public partial class ViewImports : IXncfTemplatePage
    {
        public string RelativeFilePath => "Areas/Admin/Pages/Shared/_ViewImports.cshtml";

        public string OrgName { get; set; }
        public string XncfName { get; set; }

        public ViewImports(string orgName, string xncfName)
        {
            OrgName = orgName;
            XncfName = xncfName;
        }
    }
}
