using Senparc.Xncf.XncfBuidler.Templates;

namespace Senparc.Xncf.XncfBuidler.Templates.Models.DatabaseModel
{
    public partial class SenparcDbContextFactory : IXncfTemplatePage
    {
        /// <summary>
        /// 相对地址
        /// </summary>
        public string RelativeFilePath => $"SenparcDbContextFactory.cs";

        public string OrgName { get; set; }
        public string XncfName { get; set; }

        public SenparcDbContextFactory(string orgName, string xncfName)
        {
            OrgName = orgName;
            XncfName = xncfName;
        }
    }
}
