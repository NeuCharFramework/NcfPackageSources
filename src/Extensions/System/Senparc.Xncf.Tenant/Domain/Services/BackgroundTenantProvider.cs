/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BackgroundTenantProvider.cs
    File description: BackgroundTenantProvider related definitions and implementation

    Created by: Senparc - 20261002

    Modified by: Senparc - 20261103
    Change description: v0.15.14 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.15.17 Updated module behavior, localization metadata, and English source comments

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

        /// <summary>
        /// Initializes the background tenant provider.
        /// </summary>
        /// <param name="tenantCache">Cache containing tenant records.</param>
        public BackgroundTenantProvider(FullTenantInfoCache tenantCache)
        {
            _tenantCache = tenantCache;
        }

        /// <summary>
        /// Loads enabled tenant records and converts them into matched background request contexts.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel cache access.</param>
        /// <returns>Enabled tenant contexts ordered by the cache provider.</returns>
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
