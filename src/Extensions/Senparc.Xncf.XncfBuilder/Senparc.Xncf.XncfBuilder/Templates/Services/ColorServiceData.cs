using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Templates.Services
{
    public partial class ColorService : IXncfTemplatePage
    {
        /// <summary>
        /// 相对地址
        /// </summary>
        public string RelativeFilePath => $"Services\\ColorService.cs";

        public string OrgName { get; set; }
        public string XncfName { get; set; }


        public ColorService(string orgName, string xncfName)
        {
            OrgName = orgName;
            XncfName = xncfName;
        }
    }
}
