using Senparc.Xncf.XncfBuidler.Templates;

namespace Senparc.Xncf.XncfBuidler.Templates.Models.DatabaseModel
{
    public partial class MySenparcEntities : IXncfTemplatePage
    {
        /// <summary>
        /// 相对地址
        /// </summary>
        public string RelativeFilePath => $"Models\\DatabaseModel\\{XncfName}SenparcEntities.cs";

        public string OrgName { get; set; }
        public string XncfName { get; set; }

        public bool UseSample { get; set; }

        public MySenparcEntities(string orgName, string xncfName,bool useSample)
        {
            OrgName = orgName;
            XncfName = xncfName;
            UseSample = useSample;
        }
    }
}
