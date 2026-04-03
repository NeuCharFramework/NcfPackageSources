using Senparc.Ncf.Repository;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.Models;

namespace Senparc.Xncf.Tenant.ACL.Repository
{
    /// <summary>
    /// Private TenantInfoClientRepositoryBase for tenant TenantInfo
    /// </summary>
    public class TenantInfoRepository : ClientRepositoryBase<TenantInfo>
    {
        public TenantInfoRepository(ITenantInfoDbData db) : base(db)
        {
        }
    }
}
