using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuidler.Templates.Migrations
{
    public partial class SenparcEntitiesModelSnapshotForInit : IXncfTemplatePage
    {
        /// <summary>
        /// 相对地址
        /// </summary>
        public string RelativeFilePath => $"Migrations/{XncfName}SenparcEntitiesModelSnapshot.cs";

        public string OrgName { get; set; }
        public string XncfName { get; set; }

        public SenparcEntitiesModelSnapshotForInit(string orgName, string xncfName)
        {
            OrgName = orgName;
            XncfName = xncfName;
        }
    }
}
