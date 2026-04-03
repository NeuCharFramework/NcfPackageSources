using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using System;

namespace Senparc.Ncf.Core.MultiTenant
{
    /// <summary>
    /// MultiTenantHelper
    /// </summary>
    public static class MultiTenantHelper
    {
        /// <summary>
        /// Get the RequestTenantInfo and check if it matches
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="referenceMethod">Method that references this method (used for exception messages)</param>
        /// <param name="dbContext"></param>
        /// <exception cref="NcfTenantException">Thrown when RequestTenantInfo.MatchSuccess is false</exception>
        /// <returns></returns>
        public static RequestTenantInfo TryGetAndCheckRequestTenantInfo(IServiceProvider serviceProvider, string referenceMethod, DbContext dbContext = null)
        {
            var requestTenantInfo = serviceProvider.GetRequiredService<RequestTenantInfo>();
            //Console.WriteLine($"{referenceMethod} requestTenantInfo:" + requestTenantInfo.GetHashCode());

            if (!SiteConfig.IsInstalling)
            {
                //If not set, set it
               //if (!requestTenantInfo.TriedMatching)
               // {
               //     throw new NcfUninstallException("TriedMatching is false, it is assumed that the system has not been installed. If you see this message in debugging mode (F5), please ignore it and continue execution.", null);
               // }

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
