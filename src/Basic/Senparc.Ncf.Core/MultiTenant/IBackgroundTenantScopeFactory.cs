/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：IBackgroundTenantScopeFactory.cs
    文件功能描述：IBackgroundTenantScopeFactory 相关定义与实现


    创建标识：Senparc - 20261002

    修改标识：Senparc - 20261103
    修改描述：v0.30.3-preview9 提供请求租户快照与后台租户作用域，支持隔离作用域恢复租户上下文

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
