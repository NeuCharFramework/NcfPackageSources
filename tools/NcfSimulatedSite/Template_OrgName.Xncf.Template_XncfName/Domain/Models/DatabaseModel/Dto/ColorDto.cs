using Senparc.Ncf.Core.Models;

namespace Template_OrgName.Xncf.Template_XncfName.Domain.Models.DatabaseModel.Dto
{
    public class ColorDto : DtoBase
    {
        /// <summary>
        /// Color code, 0-255
        /// </summary>
        public int Red { get; set; }
        /// <summary>
        /// Color code, 0-255
        /// </summary>
        public int Green { get; set; }
        /// <summary>
        /// Color code, 0-255
        /// </summary>
        public int Blue { get; set; }

        /// <summary>
        /// Additional columns, test the database multiple times Migrate
        /// </summary>
        public string AdditionNote { get; set; }

        public ColorDto() { }
    }
}
