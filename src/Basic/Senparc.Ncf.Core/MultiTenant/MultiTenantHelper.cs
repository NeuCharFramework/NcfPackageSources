/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MultiTenantHelper.cs
    File description: MultiTenantHelper related implementation

    Created by: Senparc - 20210217

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using System;

namespace Senparc.Ncf.Core.MultiTenant
{
    /// <summary>
    /// Validates and retrieves the tenant context associated with the current request.
    /// </summary>
    /// <remarks>
    /// Installation mode bypasses the normal match-success requirement because tenant
    /// services may not yet be fully initialized while the application is being set up.
    /// </summary>
    public static class MultiTenantHelper
    {
        /// <summary>
        /// Resolves the current request tenant and validates that matching succeeded
        /// outside installation mode.
        /// </summary>
        /// <param name="serviceProvider">The provider used to resolve request tenant state.</param>
        /// <param name="referenceMethod">The caller name included in diagnostics.</param>
        /// <param name="dbContext">Optional context associated with the validation.</param>
        /// <returns>The resolved request tenant information.</returns>
        /// <exception cref="NcfTenantException">Thrown when tenant matching failed outside installation.</exception>
        public static RequestTenantInfo TryGetAndCheckRequestTenantInfo(IServiceProvider serviceProvider, string referenceMethod, DbContext dbContext = null)
        {
            var requestTenantInfo = serviceProvider.GetRequiredService<RequestTenantInfo>();
            //Console.WriteLine($"{referenceMethod} requestTenantInfo:" + requestTenantInfo.GetHashCode());

            if (!SiteConfig.IsInstalling)
            {
                if (!requestTenantInfo.MatchSuccess)
                {
                    var errorMsg = SiteConfig.SenparcCoreSetting.EnableMultiTenant ? $"当前多租户状态已经启用，但在 {referenceMethod} 调用过程中发现未捕捉到正确的 RequestTenantInfo.TenantId" : "当前多租户状态未开启";

                    throw new NcfTenantException(errorMsg, new NcfDatabaseException($"本次请求没有匹配到正确的多租户 RequestTenantInfo 信息。Id：{requestTenantInfo.Id}，Name：{requestTenantInfo.Name}",
                        DatabaseConfigurationFactory.Instance.Current.GetType(), dbContext?.GetType()));
                }
            }
            return requestTenantInfo;
        }
    }
}
