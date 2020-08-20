using Senparc.Xncf.XncfBuidler.Templates;

namespace Senparc.Xncf.XncfBuidler.Templates.Models.DatabaseModel
{
    public partial class SenparcEntities : IXncfTemplatePage
    {
        /// <summary>
        /// 相对地址
        /// </summary>
        public string RelativeFilePath => $"Models\\{XncfName}SenparcEntities.cs";

        public string OrgName { get; set; }
        public string XncfName { get; set; }

        public SenparcEntities(string orgName, string xncfName)
        {
            OrgName = orgName;
            XncfName = xncfName;
        }
    }
}
