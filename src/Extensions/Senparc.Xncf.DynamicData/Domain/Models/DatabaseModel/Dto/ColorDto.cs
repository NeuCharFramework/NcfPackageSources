using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.DynamicData.Models.DatabaseModel.Dto
{
    public class ColorDto : DtoBase
    {
        /// <summary>
        /// Color code, 0-255
        /// </summary>
        public int Red { get; private set; }
        /// <summary>
        /// Color code, 0-255
        /// </summary>
        public int Green { get; private set; }
        /// <summary>
        /// Color code, 0-255
        /// </summary>
        public int Blue { get; private set; }

        private ColorDto() { }
    }
}
