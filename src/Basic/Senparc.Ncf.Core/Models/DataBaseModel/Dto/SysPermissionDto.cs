/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysPermissionDto.cs
    File description: SysPermissionDto related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    public class SysPermissionDto : DtoBase
    {
        public string RoleId { get; set; }

        public string PermissionId { get; set; }

        public bool IsMenu { get; set; }

        /// <summary>
        /// role code
        /// </summary>
        public string RoleCode { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ResourceCode { get; set; }
    }
}
