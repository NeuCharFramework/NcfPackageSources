/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: AdminAuthConfigRepository.cs
    File description: Related implementation.
    
    
    Created by: Senparc - 20260705
    
    Modified by: Senparc - 20260705
    Change description: v0.0.3 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.0.4 Updated module behavior and related support.
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
        private AdminAuthConfigRepository() : base(null)
        {
        }

        public AdminAuthConfigRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {
        }
    }
}
