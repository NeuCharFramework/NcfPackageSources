using System;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.MultiTenant;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    internal static class TenantMonitorScope
    {
        public static int GetTenantId(IServiceProvider serviceProvider)
        {
            return SiteConfig.SenparcCoreSetting.EnableMultiTenant
                ? MultiTenantHelper.TryGetAndCheckRequestTenantInfo(serviceProvider, nameof(TenantMonitorScope)).Id
                : 0;
        }
    }
}
