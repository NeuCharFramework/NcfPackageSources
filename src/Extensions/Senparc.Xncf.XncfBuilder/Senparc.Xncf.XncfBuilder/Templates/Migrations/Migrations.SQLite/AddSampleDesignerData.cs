using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Templates.Migrations.Migrations.SQLite
{
    public partial class AddSampleDesigner : IXncfTemplatePage
    {
        /// <summary>
        /// 相对地址
        /// </summary>
        public string RelativeFilePath => $"Migrations/Migrations.SQLite/{MigrationTime}_AddSample.Designer.cs";

        public string OrgName { get; set; }
        public string XncfName { get; set; }
        public string MigrationTime { get; set; }

        public AddSampleDesigner(string orgName, string xncfName, string migrationTime)
        {
            OrgName = orgName;
            XncfName = xncfName;
            MigrationTime = migrationTime;
        }
    }
}
