using Senparc.Ncf.Core.Models;

namespace Template_OrgName.Xncf.Template_XncfName.Models.DatabaseModel.Dto
{
    public class ColorDto : DtoBase
    {
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Red { get; set; }
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Green { get; set; }
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Blue { get; set; }

        /// <summary>
        /// 附加列，测试多次数据库 Migrate
        /// </summary>
        public string AdditionNote { get; set; }

        public ColorDto() { }
    }
}
