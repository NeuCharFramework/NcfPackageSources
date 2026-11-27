/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysRoleAdminUserInfoDto.cs
    File description: SysRoleAdminUserInfoDto related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    public class SysRoleAdminUserInfoDto : DtoBase
    {
        /// <summary>
        /// role name
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>Gets or sets the role identifier.</summary>
        public string RoleId { get; set; }

        /// <summary>Gets or sets the administrator account identifier.</summary>
        public int AdminAccountId { get; set; }

        /// <summary>Gets or sets whether the administrator currently has the role.</summary>
        public bool HasRole { get; set; }
    }
}
