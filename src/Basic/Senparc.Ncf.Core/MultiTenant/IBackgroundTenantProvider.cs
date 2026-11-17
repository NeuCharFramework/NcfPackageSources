/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IBackgroundTenantProvider.cs
    File description: IBackgroundTenantProvider related definitions and implementation

    Created by: Senparc - 20261002

    Modified by: Senparc - 20261103
    Change description: v0.30.3-preview9 Updated module behavior and related support.

----------------------------------------------------------------*/

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.MultiTenant
{
    /// <summary>Supplies enabled tenants using the system's existing tenant registry.</summary>
    public interface IBackgroundTenantProvider
    {
        Task<IReadOnlyList<RequestTenantInfo>> GetEnabledTenantsAsync(CancellationToken cancellationToken);
    }
}
