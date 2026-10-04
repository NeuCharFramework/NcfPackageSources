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
