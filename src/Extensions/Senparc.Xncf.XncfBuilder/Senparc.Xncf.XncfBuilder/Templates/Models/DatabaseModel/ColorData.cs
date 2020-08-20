using Senparc.Xncf.XncfBuidler.Templates;

namespace Senparc.Xncf.XncfBuidler.Templates.Models.DatabaseModel
{
    public partial class Color : IXncfTemplatePage
    {
        /// <summary>
        /// 相对地址
        /// </summary>
        public string RelativeFilePath => $"Models\\DatabaseModel\\Color.cs";

        public string OrgName { get; set; }
        public string XncfName { get; set; }


        public Color(string orgName, string xncfName)
        {
            OrgName = orgName;
            XncfName = xncfName;
        }
    }
}
