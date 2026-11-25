/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: ColorDto.cs
    File description: ColorDto related implementation

    Created by: Senparc - 20200820

    Modified by: Senparc - 20261119
    Change description: v1.1.15 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261124
    Change description: v1.1.15 Integrated localization, tooling, and runtime improvements across the module.

----------------------------------------------------------------*/

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
