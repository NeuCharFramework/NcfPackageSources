using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.MultiTenant
{
    /// <summary>
    /// 多租户中间件
    /// </summary>
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var serviceProvider = context.RequestServices;
            var requestTenantInfo = serviceProvider.GetRequiredService<RequestTenantInfo>();

            string tenantName = null;
            TenantInfoDto tenantInfoDto = null;
            switch (Config.SiteConfig.SenparcCoreSetting.TenantRule)
            {
                case TenantRule.DomainName:
                    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                    var httpContext = httpContextAccessor.HttpContext;
                    var urlData = httpContext.Request;
                    var host = urlData.Host.Host.ToUpper()/*全部大写*/;//主机名（不带端口）

                    var fullTenantInfoCache = serviceProvider.GetRequiredService<FullTenantInfoCache>();
                    var tenantInfoCollection = fullTenantInfoCache.Data;

                    if (!tenantInfoCollection.TryGetValue(host, out tenantInfoDto))
                    {
                        goto default;
                    }
                    break;
                case TenantRule.RequestHeader:
                    tenantName = context.Request.Headers["TenantName"];
                    if (string.IsNullOrEmpty(tenantName))
                    {
                        goto default;
                    }
                    break;
                default:
                    tenantName = SiteConfig.TENANT_DEFAULT_NAME;
                    break;
            }

            requestTenantInfo.Id = tenantInfoDto.TenantId;
            requestTenantInfo.Name = tenantInfoDto.Name;

            await _next(context);
        }
    }
}
