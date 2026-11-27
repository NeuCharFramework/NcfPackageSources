/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminUserInfoRepository.cs
    File description: Senparc.Areas.Admin.ACL related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL
{
    public interface IAdminUserInfoRepository : IClientRepositoryBase<AdminUserInfo>
    {
    }

    public class AdminUserInfoRepository : ClientRepositoryBase<AdminUserInfo>, IAdminUserInfoRepository
    {
        /// <summary>Reserved parameterless constructor for repository infrastructure.</summary>
        private AdminUserInfoRepository() : base(null) { }

        /// <summary>Initializes the administrator repository with the NCF database provider.</summary>
        /// <param name="ncfDbData">The database data provider used by the base repository.</param>
        public AdminUserInfoRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {

        }
    }
}

