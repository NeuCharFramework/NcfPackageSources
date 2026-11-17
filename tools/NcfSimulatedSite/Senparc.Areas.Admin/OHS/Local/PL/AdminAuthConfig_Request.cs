/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: AdminAuthConfig_Request.cs
    File description: Related implementation.
    
    
    Created by: Senparc - 20260705
    
    Modified by: Senparc - 20260705
    Change description: v0.0.3 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.0.4 Updated module behavior and related support.
----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.XncfBase.FunctionRenders;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Areas.Admin.OHS.PL
{
    public class AdminAuthConfig_SetExpireSettingsRequest : FunctionAppRequestBase
    {
        [Range(5, 525600)]
        public int AdminWebLoginExpireMinutes { get; set; } = AdminAuthConfig.DefaultAdminWebLoginExpireMinutes;

        [Range(5, 525600)]
        public int BackendJwtExpireMinutes { get; set; } = AdminAuthConfig.DefaultBackendJwtExpireMinutes;
    }
}
