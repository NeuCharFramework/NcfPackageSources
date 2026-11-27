/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatSessionModuleRepository.cs
    File description: Senparc.Areas.Admin.ACL related implementation

    Created by: Senparc - 20260325

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL
{
    public interface IAdminChatSessionModuleRepository : IClientRepositoryBase<AdminChatSessionModule>
    {
    }

    public class AdminChatSessionModuleRepository : ClientRepositoryBase<AdminChatSessionModule>, IAdminChatSessionModuleRepository
    {
        /// <summary>Reserved parameterless constructor for repository infrastructure.</summary>
        private AdminChatSessionModuleRepository() : base(null) { }

        /// <summary>Initializes the session-module repository with the NCF database provider.</summary>
        /// <param name="ncfDbData">The database data provider used by the base repository.</param>
        public AdminChatSessionModuleRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {
        }
    }
}
