/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IBackgroundTenantScopeFactory.cs
    File description: IBackgroundTenantScopeFactory related definitions and implementation

    Created by: Senparc - 20261002

    Modified by: Senparc - 20261103
    Change description: v0.30.3-preview9 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

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
