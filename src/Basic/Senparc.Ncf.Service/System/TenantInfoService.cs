using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Service
{
    public class TenantInfoService : ClientServiceBase<TenantInfo>
    {
        private readonly Lazy<IHttpContextAccessor> _httpContextAccessor;

        public TenantInfoService(IClientRepositoryBase<TenantInfo> repo, IServiceProvider serviceProvider, Lazy<IHttpContextAccessor> httpContextAccessor)
            : base(repo, serviceProvider)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 设置当前 Request 范围内的 RequestTenantInfo 值
        /// </summary>
        /// <param name="httpContext">如果为 null，则自动从 IHttpContextAccessor 中获取</param>
        /// <returns></returns>
        public async Task<RequestTenantInfo> SetScopedRequestTenantInfoAsync(HttpContext httpContext)
        {
            var requestTenantInfo = _serviceProvider.GetRequiredService<RequestTenantInfo>();
            httpContext = httpContext ?? _httpContextAccessor.Value.HttpContext;
            string tenantKey = null;
            switch (SiteConfig.SenparcCoreSetting.TenantRule)
            {
                case TenantRule.DomainName:
                    Console.WriteLine("进入到 TenantRule.DomainName");
                    var urlData = httpContext.Request;
                    var host = urlData.Host.Host.ToUpper()/*全部大写*/;//主机名（不带端口）
                    tenantKey = host;
                    break;
                case TenantRule.RequestHeader:
                    var headerTenantKey = httpContext.Request.Headers["TenantKey"];
                    if (!string.IsNullOrEmpty(headerTenantKey))
                    {
                        tenantKey = headerTenantKey[0].ToUpper();
                    }
                    break;
                default:
                    throw new Exception("未处理的 TenantRule 类型");
            }

            var fullTenantInfoCache = _serviceProvider.GetRequiredService<FullTenantInfoCache>();
            var tenantInfoCollection = await fullTenantInfoCache.GetDataAsync();
            Console.WriteLine($"tenantInfoCollection：{tenantInfoCollection.ToJson()}");
            Console.WriteLine($"tenantKey：{tenantKey}");

            if (!string.IsNullOrEmpty(tenantKey) && tenantInfoCollection.TryGetValue(tenantKey, out var tenantInfoDto))
            {
                Console.WriteLine($"找到匹配缓存：{tenantInfoDto.ToJson()}");
                requestTenantInfo.Id = tenantInfoDto.TenantId;
                requestTenantInfo.Name = tenantInfoDto.Name;
                requestTenantInfo.TenantKey = tenantInfoDto.TenantKey;
            }
            else
            {
                Console.WriteLine($"未找到匹配缓存");
                requestTenantInfo.Name = SiteConfig.TENANT_DEFAULT_NAME;
            }
            Console.WriteLine($"requestTenantInfo：{requestTenantInfo.ToJson()}");

            return requestTenantInfo;
        }

        /// <summary>
        /// 创建租户信息
        /// </summary>
        /// <param name="name"></param>
        /// <param name="tenantKey"></param>
        /// <returns></returns>
        public async Task<TenantInfoDto> CreateTenantInfoAsync(string name, string tenantKey)
        {
            tenantKey = tenantKey.ToUpper();
            var tenantInfo = await GetObjectAsync(z => z.Name == name || z.TenantKey == tenantKey);
            if (tenantInfo != null)
            {
                throw new NcfTenantException($"已存在名为 {name} 或 关键条件为 {tenantKey} 的租户记录！");
            }

            tenantInfo = new TenantInfo(name, true, tenantKey);
            await SaveObjectAsync(tenantInfo);

            //所有涉及到租户信息的修改，都清除租户信息，重新更新
            var fullTenantInfoCache = _serviceProvider.GetRequiredService<FullTenantInfoCache>();
            await fullTenantInfoCache.RemoveCacheAsync();
            //TODO:可以放到充血实体领域中完成

            return base.Mapper.Map<TenantInfoDto>(tenantInfo);
        }

        /// <summary>
        /// 创建默认 TenantInfo 信息
        /// </summary>
        /// <param name="httpContext">如果为 null，则自动从 IHttpContextAccessor 中获取</param>
        /// <returns></returns>
        public async Task<TenantInfoDto> CreateInitTenantInfoAsync(HttpContext httpContext)
        {
            httpContext = httpContext ?? _httpContextAccessor.Value.HttpContext;
            var urlData = httpContext.Request;
            var host = urlData.Host.Host;
            return await CreateTenantInfoAsync(host, host);
        }

        public override async Task SaveChangesAsync()
        {
         
            await base.SaveChangesAsync();
        }
    }
}
