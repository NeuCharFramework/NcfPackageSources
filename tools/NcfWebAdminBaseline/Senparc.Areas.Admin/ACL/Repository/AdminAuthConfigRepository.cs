/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminAuthConfigRepository.cs
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

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL
{
    public interface IAdminAuthConfigRepository : IClientRepositoryBase<AdminAuthConfig>
    {
    }

    public class AdminAuthConfigRepository : ClientRepositoryBase<AdminAuthConfig>, IAdminAuthConfigRepository
    {
        /// <summary>Reserved parameterless constructor for repository infrastructure.</summary>
        private AdminAuthConfigRepository() : base(null)
        {
        }

        /// <summary>Initializes the authentication-config repository with the NCF database provider.</summary>
        /// <param name="ncfDbData">The database data provider used by the base repository.</param>
        public AdminAuthConfigRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {
        }
    }
}
