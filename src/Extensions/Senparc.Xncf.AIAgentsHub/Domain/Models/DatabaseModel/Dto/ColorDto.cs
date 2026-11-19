/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColorDto.cs
    File description: ColorDto related implementation

    Created by: Senparc - 20240311

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.13.16 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.AIAgentsHub.Models.DatabaseModel.Dto
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
