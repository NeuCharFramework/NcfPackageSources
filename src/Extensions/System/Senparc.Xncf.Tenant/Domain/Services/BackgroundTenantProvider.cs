/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：BackgroundTenantProvider.cs
    文件功能描述：BackgroundTenantProvider 相关定义与实现


    创建标识：Senparc - 20261002

    修改标识：Senparc - 20261103
    修改描述：v0.15.14 提供后台租户发现、租户缓存与请求作用域支持

----------------------------------------------------------------*/

using Senparc.Ncf.Core.MultiTenant;
using Senparc.Xncf.Tenant.Domain.Cache;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.Tenant.Domain.Services
{
    public sealed class BackgroundTenantProvider : IBackgroundTenantProvider
    {
        private readonly FullTenantInfoCache _tenantCache;

        public BackgroundTenantProvider(FullTenantInfoCache tenantCache)
        {
            _tenantCache = tenantCache;
        }

        public async Task<IReadOnlyList<RequestTenantInfo>> GetEnabledTenantsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tenants = await _tenantCache.GetDataAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return tenants.Values.Select(z =>
            {
                var tenant = new RequestTenantInfo { Id = z.Id, Name = z.Name, TenantKey = z.TenantKey };
                tenant.TryMatch(true);
                return tenant;
            }).ToList();
        }
    }
}
