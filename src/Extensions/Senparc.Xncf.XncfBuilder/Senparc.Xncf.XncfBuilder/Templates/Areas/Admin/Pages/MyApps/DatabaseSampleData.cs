using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuidler.Templates.Areas.Admin.Pages.MyApps
{
    public partial class DatabaseSample : IXncfTemplatePage
    {
        public string RelativeFilePath => $"Areas/Admin/Pages/{XncfName}/DatabaseSample.cshtml";

        public string OrgName { get; set; }
        public string XncfName { get; set; }

        public string MenuName { get; set; }

        public DatabaseSample(string orgName, string xncfName, string menuName)
        {
            OrgName = orgName;
            XncfName = xncfName;
            MenuName = menuName;
        }
    }
}
