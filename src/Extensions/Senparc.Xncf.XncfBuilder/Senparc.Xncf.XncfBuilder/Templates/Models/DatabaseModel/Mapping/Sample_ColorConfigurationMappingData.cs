using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuidler.Templates.Models.DatabaseModel.Mapping
{
    public partial class Sample_ColorConfigurationMapping : IXncfTemplatePage
    {
        /// <summary>
        /// 相对地址
        /// </summary>
        public string RelativeFilePath => $"Models\\DatabaseModel\\Mapping\\{XncfName}_ColorConfigurationMapping.cs";

        public string OrgName { get; set; }
        public string XncfName { get; set; }

        public Sample_ColorConfigurationMapping(string orgName, string xncfName)
        {
            OrgName = orgName;
            XncfName = xncfName;
        }
    }
}
