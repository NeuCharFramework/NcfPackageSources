/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TenantMonitorScope.cs
    File description: TenantMonitorScope related definitions and implementation

    Created by: Senparc - 20261001

    Modified by: Senparc - 20261103
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.MultiTenant;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    internal static class TenantMonitorScope
    {
        /// <summary>
        /// Resolves the current request tenant identifier for token monitoring.
        /// Single-tenant deployments use tenant identifier zero.
        /// </summary>
        /// <param name="serviceProvider">The request service provider.</param>
        /// <returns>The current tenant identifier, or zero when multi-tenancy is disabled.</returns>
        public static int GetTenantId(IServiceProvider serviceProvider)
        {
            return SiteConfig.SenparcCoreSetting.EnableMultiTenant
                ? MultiTenantHelper.TryGetAndCheckRequestTenantInfo(serviceProvider, nameof(TenantMonitorScope)).Id
                : 0;
        }
    }
}
