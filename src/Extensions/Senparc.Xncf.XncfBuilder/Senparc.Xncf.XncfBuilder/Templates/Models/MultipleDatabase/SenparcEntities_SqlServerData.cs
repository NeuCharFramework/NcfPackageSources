using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Templates.Models.MultipleDatabase
{
   public partial class SenparcEntities_SqlServer : IXncfTemplatePage
    {
        public string RelativeFilePath => $"Models/MultipleDatabase/{XncfName}SenparcEntities_SqlServer.cs";

        public string OrgName { get; set; }
        public string XncfName { get; set; }

        public SenparcEntities_SqlServer(string orgName, string xncfName)
        {
            OrgName = orgName;
            XncfName = xncfName;
        }
    }
}
