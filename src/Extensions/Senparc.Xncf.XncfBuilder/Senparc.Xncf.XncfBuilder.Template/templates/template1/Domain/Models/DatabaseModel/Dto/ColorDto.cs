using Senparc.Ncf.Core.Models;

namespace Template_OrgName.Xncf.Template_XncfName.Domain.Models.DatabaseModel.Dto
{
    public class ColorDto : DtoBase
    {
        /// <summary>
        /// color code, 0-255
        /// </summary>
        public int Red { get; set; }
        /// <summary>
        /// color code, 0-255
        /// </summary>
        public int Green { get; set; }
        /// <summary>
        /// color code, 0-255
        /// </summary>
        public int Blue { get; set; }

        /// <summary>
        /// additional columns; test multiple database migrations
        /// </summary>
        public string AdditionNote { get; set; }

        public ColorDto() { }
    }
}
