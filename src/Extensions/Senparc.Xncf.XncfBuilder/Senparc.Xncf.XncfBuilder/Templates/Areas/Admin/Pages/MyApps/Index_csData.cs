using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuidler.Templates.Areas.Admin.Pages.MyApps
{
    public partial class Index_cs : IXncfTemplatePage
    {
        public string RelativeFilePath => $"Areas/Admin/Pages/{XncfName}/Index.cshtml.cs";

        public string OrgName { get; set; }
        public string XncfName { get; set; }

        public Index_cs(string orgName, string xncfName)
        {
            OrgName = orgName;
            XncfName = xncfName;
        }
    }
}
