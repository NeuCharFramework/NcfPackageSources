/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TenantInfoRepository.cs
    File description: TenantInfoRepository related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.Ncf.Repository;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.Models;

namespace Senparc.Xncf.Tenant.ACL.Repository
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class TenantInfoRepository : ClientRepositoryBase<TenantInfo>
    {
        public TenantInfoRepository(ITenantInfoDbData db) : base(db)
        {
        }
    }
}
