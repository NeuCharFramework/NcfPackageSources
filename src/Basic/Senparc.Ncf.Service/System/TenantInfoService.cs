using Microsoft.AspNetCore.Http;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Service
{
    public class TenantInfoService : ClientServiceBase<TenantInfo>
    {
        private readonly Lazy<IHttpContextAccessor> httpContextAccessor;

        public TenantInfoService(IClientRepositoryBase<TenantInfo> repo, IServiceProvider serviceProvider, Lazy<IHttpContextAccessor> httpContextAccessor)
            : base(repo, serviceProvider)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 创建默认 TenantInfo 信息
        /// </summary>
        /// <returns></returns>
        public async Task<TenantInfo> CreateInitTenantInfo()
        {
            var httpContext = httpContextAccessor.Value.HttpContext;
            var urlData = httpContext.Request;
            var host = urlData.Host.Host;
            TenantInfo tenantInfo = new TenantInfo(host, true, host.ToUpper());
            await SaveObjectAsync(tenantInfo);
            return tenantInfo;
        }
    }
}
