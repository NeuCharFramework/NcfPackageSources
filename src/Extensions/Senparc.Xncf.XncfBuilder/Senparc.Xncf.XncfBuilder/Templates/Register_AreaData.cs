using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuidler.Templates
{
    public partial class Register_Area : IXncfTemplatePage
    {
        /// <summary>
        /// 相对地址
        /// </summary>
        public string RelativeFilePath => "Register.Area.cs";

        public string OrgName { get; set; }
        public string XncfName { get; set; }

        public Register_Area(string orgName, string xncfName)
        {
            OrgName = orgName;
            XncfName = xncfName;
        }
    }
}
