/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TenantInfoRepository.cs
    File description: TenantInfoRepository related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.15.17 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Repository;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.Models;

namespace Senparc.Xncf.Tenant.ACL.Repository
{
    /// <summary>
    /// Repository for persisted tenant registrations.
    /// </summary>
    public class TenantInfoRepository : ClientRepositoryBase<TenantInfo>
    {
        /// <summary>
        /// Initializes the repository with tenant database access.
        /// </summary>
        /// <param name="db">The tenant database wrapper.</param>
        public TenantInfoRepository(ITenantInfoDbData db) : base(db)
        {
        }
    }
}
