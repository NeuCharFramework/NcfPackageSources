using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuidler.Templates.Areas.Admin.Pages.MyApps
{
    public partial class Index : IXncfTemplatePage
    {
        public string RelativeFilePath => $"Areas/Admin/Pages/{XncfName}/Index.cshtml";

        public string OrgName { get; set; }
        public string XncfName { get; set; }

        public Index(string orgName, string xncfName)
        {
            OrgName = orgName;
            XncfName = xncfName;
        }
    }
}
