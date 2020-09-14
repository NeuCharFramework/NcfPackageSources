using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Templates
{
    public partial class csproj: IXncfTemplatePage
    {
        public string RelativeFilePath => $"{OrgName}.Xncf.{XncfName}.csproj";

        public string OrgName { get; set; }
        public string XncfName { get; set; }
        public string Version { get; set; }
        public string MenuName { get; set; }
        public string Description { get; set; }

        public bool UseWeb { get; set; }
        public bool UseDatabase { get; set; }

        /// <summary>
        /// 项目文件路径（相对于src目录）
        /// </summary>
        public string ProjectFilePath => $"{OrgName}.Xncf.{XncfName}\\{OrgName}.Xncf.{XncfName}.csproj";
    }
}
