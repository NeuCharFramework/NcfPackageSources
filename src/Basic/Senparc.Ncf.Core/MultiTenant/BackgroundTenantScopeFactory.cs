using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.MultiTenant
{
    public sealed class BackgroundTenantScopeFactory : IBackgroundTenantScopeFactory
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public BackgroundTenantScopeFactory(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task ForEachEnabledTenantAsync(
            Func<IServiceProvider, CancellationToken, Task> action,
            CancellationToken cancellationToken = default)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            using var discoveryScope = _scopeFactory.CreateScope();
            var tenants = await GetEnabledTenantsAsync(discoveryScope.ServiceProvider,
                SiteConfig.SenparcCoreSetting.EnableMultiTenant, cancellationToken).ConfigureAwait(false);
            foreach (var tenant in tenants)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var scope = CreateInitializedScope(tenant);
                await action(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        public async Task<IServiceScope> TryCreateScopeAsync(int tenantId, CancellationToken cancellationToken = default)
        {
            var multiTenantEnabled = SiteConfig.SenparcCoreSetting.EnableMultiTenant;
            using var discoveryScope = _scopeFactory.CreateScope();
            var tenants = await GetEnabledTenantsAsync(discoveryScope.ServiceProvider,
                multiTenantEnabled, cancellationToken).ConfigureAwait(false);
            var tenant = multiTenantEnabled
                ? tenants.FirstOrDefault(z => z.Id == tenantId)
                : tenants.Single();
            cancellationToken.ThrowIfCancellationRequested();
            return tenant == null ? null : CreateInitializedScope(tenant);
        }

        private static async Task<IReadOnlyList<RequestTenantInfo>> GetEnabledTenantsAsync(
            IServiceProvider serviceProvider, bool multiTenantEnabled, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!multiTenantEnabled)
            {
                var tenant = new RequestTenantInfo { Name = SiteConfig.TENANT_DEFAULT_NAME };
                tenant.TryMatch(true);
                return new[] { tenant };
            }

            var provider = serviceProvider.GetService<IBackgroundTenantProvider>()
                ?? throw new NcfTenantException("Multi-tenant background execution requires an IBackgroundTenantProvider.");
            var tenants = await provider.GetEnabledTenantsAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (tenants == null || tenants.Any(z => z == null || z.Id <= 0 || !z.MatchSuccess)
                || tenants.Select(z => z.Id).Distinct().Count() != tenants.Count)
            {
                throw new NcfTenantException("The background tenant provider returned invalid or duplicate tenant information.");
            }

            return tenants.OrderBy(z => z.Id).ToList();
        }

        private IServiceScope CreateInitializedScope(RequestTenantInfo tenant)
        {
            var scope = _scopeFactory.CreateScope();
            try
            {
                var scopedTenant = scope.ServiceProvider.GetRequiredService<RequestTenantInfo>();
                scopedTenant.Id = tenant.Id;
                scopedTenant.Name = tenant.Name;
                scopedTenant.TenantKey = tenant.TenantKey;
                scopedTenant.TryMatch(tenant.MatchSuccess);
                return scope;
            }
            catch
            {
                scope.Dispose();
                throw;
            }
        }
    }
}
