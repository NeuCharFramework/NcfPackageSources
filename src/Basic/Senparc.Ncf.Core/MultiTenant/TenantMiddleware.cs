using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
            var tenantInfo = context.RequestServices.GetRequiredService<RequestTenantInfo>();

            string tenantName = null;
            switch (Config.SiteConfig.SenparcCoreSetting.TenantRule)
            {
                case TenantRule.DomainName:
                    
                    break;
                case TenantRule.RequestHeader:
                    tenantName = context.Request.Headers["TenantName"];
                    if (string.IsNullOrEmpty(tenantName))
                    {
                        goto default;
                    }
                    break;
                default:
                    tenantName = "Default";
                    break;
            }

            tenantInfo.Name = tenantName;

            await _next(context);
        }
    }
}
