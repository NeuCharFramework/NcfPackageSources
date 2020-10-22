using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Templates.Migrations.Migrations.SQLite
{
    public partial class SenparcEntitiesModelSnapshotForInit : IXncfTemplatePage
    {
        public string RelativeFilePath => $"Migrations/Migrations.SQLite/{XncfName}SenparcEntitiesModelSnapshot.cs";

        public string OrgName { get; set; }
        public string XncfName { get; set; }

        public SenparcEntitiesModelSnapshotForInit(string orgName, string xncfName)
        {
            OrgName = orgName;
            XncfName = xncfName;
        }
    }
}
