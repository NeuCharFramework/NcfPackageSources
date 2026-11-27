/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminAuthConfig.cs
    File description: Related implementation.

    Created by: Senparc - 20260705

    Modified by: Senparc - 20260705
    Change description: v0.0.3 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.0.4 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel
{
    [Table(Register.DATABASE_PREFIX + nameof(AdminAuthConfig))]
    [Serializable]
    /// <summary>
    /// Stores the expiration windows used by administrator web login and backend JWT authentication.
    /// </summary>
    public class AdminAuthConfig : EntityBase<int>
    {
        public const int DefaultAdminWebLoginExpireMinutes = 120;
        public const int DefaultBackendJwtExpireMinutes = 150 * 60;

        public int AdminWebLoginExpireMinutes { get; private set; }
        public int BackendJwtExpireMinutes { get; private set; }

        private AdminAuthConfig()
        {
        }

        /// <summary>
        /// Initializes authentication expiration settings, replacing non-positive values with defaults.
        /// </summary>
        /// <param name="adminWebLoginExpireMinutes">The web-login lifetime in minutes.</param>
        /// <param name="backendJwtExpireMinutes">The backend JWT lifetime in minutes.</param>
        public AdminAuthConfig(int adminWebLoginExpireMinutes, int backendJwtExpireMinutes)
        {
            AdminWebLoginExpireMinutes = adminWebLoginExpireMinutes > 0
                ? adminWebLoginExpireMinutes
                : DefaultAdminWebLoginExpireMinutes;
            BackendJwtExpireMinutes = backendJwtExpireMinutes > 0
                ? backendJwtExpireMinutes
                : DefaultBackendJwtExpireMinutes;
        }

        /// <summary>
        /// Updates both authentication expiration windows and records the entity update time.
        /// Non-positive values are replaced with the corresponding defaults.
        /// </summary>
        /// <param name="adminWebLoginExpireMinutes">The web-login lifetime in minutes.</param>
        /// <param name="backendJwtExpireMinutes">The backend JWT lifetime in minutes.</param>
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
