using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Templates.App_Data.Database
{
    public partial class SenparcConfig : IXncfTemplatePage
    {
        /// <summary>
        /// 相对地址
        /// </summary>
        public string RelativeFilePath => "App_Data\\Database\\SenparcConfig.config";

        public string OrgName { get; set; }
        public string XncfName { get; set; }

        public SenparcConfig(string orgName, string xncfName)
        {
            OrgName = orgName;
            XncfName = xncfName;
        }
    }
}
