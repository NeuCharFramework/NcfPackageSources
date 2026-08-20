/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：TenantInfoRepository.cs
    文件功能描述：TenantInfoRepository 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
