using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;

namespace Senparc.Ncf.Repository
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
