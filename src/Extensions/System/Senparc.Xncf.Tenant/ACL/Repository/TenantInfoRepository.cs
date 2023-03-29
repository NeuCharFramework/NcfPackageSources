using Senparc.Ncf.Repository;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.Models;

namespace Senparc.Xncf.Tenant.ACL.Repository
{
    /// <summary>
    /// 租户 TenantInfo 的专用 TenantInfoClientRepositoryBase
    /// </summary>
    public class TenantInfoRepository : ClientRepositoryBase<TenantInfo>
    {
        public TenantInfoRepository(ITenantInfoDbData db) : base(db)
        {
        }
    }
}
