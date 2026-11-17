/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminAuthConfig.cs
    File description: Related implementation.

    Created by: Senparc - 20260705

    Modified by: Senparc - 20260705
    Change description: v0.0.3 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.0.4 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel
{
    [Table(Register.DATABASE_PREFIX + nameof(AdminAuthConfig))]
    [Serializable]
    public class AdminAuthConfig : EntityBase<int>
    {
        public const int DefaultAdminWebLoginExpireMinutes = 120;
        public const int DefaultBackendJwtExpireMinutes = 150 * 60;

        public int AdminWebLoginExpireMinutes { get; private set; }
        public int BackendJwtExpireMinutes { get; private set; }

        private AdminAuthConfig()
        {
        }

        public AdminAuthConfig(int adminWebLoginExpireMinutes, int backendJwtExpireMinutes)
        {
            AdminWebLoginExpireMinutes = adminWebLoginExpireMinutes > 0
                ? adminWebLoginExpireMinutes
                : DefaultAdminWebLoginExpireMinutes;
            BackendJwtExpireMinutes = backendJwtExpireMinutes > 0
                ? backendJwtExpireMinutes
                : DefaultBackendJwtExpireMinutes;
        }

        public void Update(int adminWebLoginExpireMinutes, int backendJwtExpireMinutes)
        {
            AdminWebLoginExpireMinutes = adminWebLoginExpireMinutes > 0
                ? adminWebLoginExpireMinutes
                : DefaultAdminWebLoginExpireMinutes;
            BackendJwtExpireMinutes = backendJwtExpireMinutes > 0
                ? backendJwtExpireMinutes
                : DefaultBackendJwtExpireMinutes;
            SetUpdateTime();
        }
    }
}
