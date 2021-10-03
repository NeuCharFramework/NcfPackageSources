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
        /// 获取 RequestTenantInfo，并且检查其是否匹配
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="referenceMethod">引用此方法的方法（用于异常提示）</param>
        /// <param name="dbContext"></param>
        /// <exception cref="NcfTenantException">如果 RequestTenantInfo.MatchSuccess 为 false，则抛出异常</exception>
        /// <returns></returns>
        public static RequestTenantInfo TryGetAndCheckRequestTenantInfo(IServiceProvider serviceProvider, string referenceMethod, DbContext dbContext = null)
        {
            var requestTenantInfo = serviceProvider.GetRequiredService<RequestTenantInfo>();
            //Console.WriteLine($"{referenceMethod} requestTenantInfo:" + requestTenantInfo.GetHashCode());

            if (!SiteConfig.IsInstalling)
            {
                //如果未设置，则进行设定
                if (!requestTenantInfo.TriedMatching)
                {
                    throw new NcfUninstallException("TriedMatching 为 false，推测系统未进行安装");
                }

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
