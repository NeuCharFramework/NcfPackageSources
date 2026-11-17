/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColorDto.cs
    File description: ColorDto related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.DynamicData.Models.DatabaseModel.Dto
{
    public class ColorDto : DtoBase
    {
        /// <summary>
        /// color code, 0-255
        /// </summary>
        public int Red { get; private set; }
        /// <summary>
        /// color code, 0-255
        /// </summary>
        public int Green { get; private set; }
        /// <summary>
        /// color code, 0-255
        /// </summary>
        public int Blue { get; private set; }

        private ColorDto() { }
    }
}
