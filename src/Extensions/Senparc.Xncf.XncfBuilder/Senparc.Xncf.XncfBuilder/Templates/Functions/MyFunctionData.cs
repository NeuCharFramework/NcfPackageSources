using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuidler.Templates.Functions
{
    public partial class MyFunction : IXncfTemplatePage
    {
        public string OrgName { get; set; }
        public string XncfName { get; set; }
        public string RelativeFilePath => "Functions/MyFunction.cs";
    }
}
