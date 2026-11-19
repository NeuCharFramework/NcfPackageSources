/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SysRole_Response.cs
    File description: SysRole_Response related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Dto;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.PL
{
    public class SysRole_Response
    {
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SysRole_GetListResponse
    {
        public IEnumerable<SysRoleListDto> List { get; set; }

        public int TotalCount { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SysRole_GetSingleResponse
    {
        public SysRoleDetailDto Detail { get; set; }
    }

    /// <summary>
    /// roleupdate
    /// </summary>
    public class SysRole_CreateOrUpdateResponse
    {
        /// <summary>
        /// 
        /// </summary>
        public string Id { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SysRole_PermissionsResponse
    {
        public IEnumerable<string> PermissionId { get; set; }
    }
}
