using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.MultiTenant
{
    public interface IBackgroundTenantScopeFactory
    {
        /// <summary>Runs sequentially in initialized, independently disposed tenant scopes.</summary>
        Task ForEachEnabledTenantAsync(
            Func<IServiceProvider, CancellationToken, Task> action,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates an initialized scope owned by the caller; returns null when the tenant is no longer enabled.
        /// Single-tenant mode creates a normal scope without consulting the tenant registry.
        /// </summary>
        Task<IServiceScope> TryCreateScopeAsync(int tenantId, CancellationToken cancellationToken = default);
    }
}
