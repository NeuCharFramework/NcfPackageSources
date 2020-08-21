using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuidler.Templates.Migrations
{
    public partial class Init : IXncfTemplatePage
    {
        /// <summary>
        /// 相对地址
        /// </summary>
        public string RelativeFilePath => $"Migrations/{MigrationTime}_Init.cs";

        public string OrgName { get; set; }
        public string XncfName { get; set; }
        public string MigrationTime { get; set; }

        public Init(string orgName, string xncfName, string migrationTime)
        {
            OrgName = orgName;
            XncfName = xncfName;
            MigrationTime = migrationTime;
        }

        public static string GetFileNamePrefix(DateTime? dateTime = null)
        {
            dateTime = dateTime ?? SystemTime.Now.DateTime;
            return dateTime.Value.ToString("yyyyMMddHHmmss");
        }
    }
}
