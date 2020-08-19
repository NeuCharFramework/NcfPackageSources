using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuidler.Templates.Areas.Admin.Pages.Shared
{
    public partial class _Layout : IXncfTemplatePage
    {
        public string RelativeFilePath => $"_{XncfName}Layout.cshtml";

        public string OrgName { get; set; }
        public string XncfName { get; set; }
    }
}
