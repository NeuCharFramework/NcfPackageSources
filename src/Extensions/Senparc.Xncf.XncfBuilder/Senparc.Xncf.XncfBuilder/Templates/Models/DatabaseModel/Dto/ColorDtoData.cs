using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuidler.Templates.Models.DatabaseModel.Dto
{
    public partial class ColorDto : IXncfTemplatePage
    {
        /// <summary>
        /// 相对地址
        /// </summary>
        public string RelativeFilePath => $"Models\\DatabaseModel\\Dto\\ColorDto.cs";

        public string OrgName { get; set; }
        public string XncfName { get; set; }

        public ColorDto(string orgName, string xncfName)
        {
            OrgName = orgName;
            XncfName = xncfName;
        }
    }
}
