/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：IBackgroundTenantProvider.cs
    文件功能描述：IBackgroundTenantProvider 相关定义与实现


    创建标识：Senparc - 20261002

    修改标识：Senparc - 20261103
    修改描述：v0.30.3-preview9 提供请求租户快照与后台租户作用域，支持隔离作用域恢复租户上下文

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
