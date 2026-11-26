/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BackgroundTenantScopeFactory.cs
    File description: BackgroundTenantScopeFactory related definitions and implementation

    Created by: Senparc - 20261002

    Modified by: Senparc - 20261103
    Change description: v0.30.3-preview9 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

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

        /// <summary>
        /// Initializes a factory that creates tenant-initialized service scopes.
        /// </summary>
        /// <param name="scopeFactory">Factory used to create dependency-injection scopes.</param>
        public BackgroundTenantScopeFactory(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Enumerates enabled tenants and executes an action inside an isolated initialized scope for each tenant.
        /// </summary>
        /// <param name="action">The operation receiving each tenant's scoped service provider.</param>
        /// <param name="cancellationToken">Token used to cancel discovery or iteration.</param>
        /// <returns>A task that completes after all enabled tenants have been processed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
        /// <exception cref="NcfTenantException">Thrown when the tenant provider returns invalid or duplicate data.</exception>
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

        /// <summary>
        /// Creates an initialized scope for a tenant when that tenant is enabled and visible.
        /// </summary>
        /// <param name="tenantId">The tenant identifier to locate in multi-tenant mode.</param>
        /// <param name="cancellationToken">Token used to cancel discovery.</param>
        /// <returns>The initialized scope, or <see langword="null"/> when no matching tenant exists.</returns>
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

        /// <summary>
        /// Resolves enabled tenants for background work. Single-tenant deployments receive
        /// a synthetic matched default tenant; multi-tenant deployments require a provider
        /// and reject null, unmatched, non-positive, or duplicate tenant records.
        /// </summary>
        /// <param name="serviceProvider">The discovery scope provider.</param>
        /// <param name="multiTenantEnabled">Whether multi-tenant discovery is enabled.</param>
        /// <param name="cancellationToken">Token used to cancel tenant discovery.</param>
        /// <returns>Enabled tenants ordered by identifier.</returns>
        /// <exception cref="NcfTenantException">Thrown when the provider is missing or returns invalid tenant data.</exception>
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

        /// <summary>
        /// Creates a dependency-injection scope and copies the selected tenant identity
        /// into the scoped <see cref="RequestTenantInfo"/> instance.
        /// </summary>
        /// <param name="tenant">The tenant snapshot used to initialize the scope.</param>
        /// <returns>An initialized service scope.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the scoped tenant service is unavailable.</exception>
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
