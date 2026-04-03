using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.Tenant.ACL.Repository;
using Senparc.Xncf.Tenant.Domain.Cache;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Tenant.Domain.Services
{
    public class TenantInfoService : ClientServiceBase<TenantInfo>
    {
        private readonly Lazy<IHttpContextAccessor> _httpContextAccessor;

        public TenantInfoService(TenantInfoRepository repo, IServiceProvider serviceProvider, Lazy<IHttpContextAccessor> httpContextAccessor)
            : base(repo, serviceProvider)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Set the RequestTenantInfo value within the current Request scope
        /// </summary>
        /// <param name="httpContext">If null, automatically obtained from IHttpContextAccessor</param>
        /// <returns></returns>
        public async Task<RequestTenantInfo> SetScopedRequestTenantInfoAsync(HttpContext httpContext)
        {
            var requestTenantInfo = _serviceProvider.GetRequiredService<RequestTenantInfo>();
            httpContext = httpContext ?? _httpContextAccessor.Value.HttpContext;
            string tenantKey = null;
            //string[] tanantCookieInfo = null;
            switch (SiteConfig.SenparcCoreSetting.TenantRule)
            {
                case TenantRule.DomainName:
                    //Console.WriteLine("\t\tEnter SetScopedRequestTenantInfoAsync -> TenantRule.DomainName");
                    var urlData = httpContext.Request;
                    var host = urlData.Host.Host.ToUpper()/*all caps*/;//Hostname (without port)
                    tenantKey = host;
                    break;
                case TenantRule.RequestHeader:
                    var headerTenantKey = httpContext.Request.Headers["TenantKey"];
                    if (!string.IsNullOrEmpty(headerTenantKey))
                    {
                        tenantKey = headerTenantKey[0].ToUpper();
                    }
                    break;
                case TenantRule.LoginInput:
                    {
                        // Check if TenantKey exists
                        var tenantKeyClaim = httpContext.User?.Claims.FirstOrDefault(c => c.Type == "TenantKey");

                        if (tenantKeyClaim == null)
                        {
                            tenantKey = null;
                            break;
                        }
                        else
                        {
                            // Read the value of TenantKey
                            tenantKey = tenantKeyClaim.Value;
                        }
                    }
                    break;
                default:
                    throw new Exception("未处理的 TenantRule 类型");
            }

            //Console.WriteLine("\t\tReady to enter _serviceProvider.GetRequiredService<FullTenantInfoCache>()");

            var fullTenantInfoCache = _serviceProvider.GetRequiredService<FullTenantInfoCache>();
            Dictionary<string, TenantInfoDto> tenantInfoCollection;
            try
            {
                tenantInfoCollection = await fullTenantInfoCache.GetDataAsync();//
            }
            catch (Exception ex)
            {
                if (!Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.EnableMultiTenant)
                {
                    //Database reading failed and no tenant was logged in
                    //tenantKey = null;
                    throw new NcfUninstallException("fullTenantInfoCache.GetDataAsync 读取失败，推测系统未安装或多租户未启用", ex);
                }

                //Database error, usually the system is not installed
                //tenantKey = null;

                throw new NcfUninstallException("fullTenantInfoCache.GetDataAsync 读取失败，推测系统未安装", ex);
            }

            //Console.WriteLine($"\t\tObtained tenantInfoCollection: {tenantInfoCollection.ToJson()}");

            if (!string.IsNullOrEmpty(tenantKey) && tenantInfoCollection.TryGetValue(tenantKey, out var tenantInfoDto))
            {
                //Console.WriteLine($"\t\t matches tenantKey: {tenantKey}");
                requestTenantInfo.Id = tenantInfoDto.Id;
                requestTenantInfo.Name = tenantInfoDto.Name;
                requestTenantInfo.TenantKey = tenantInfoDto.TenantKey;
                requestTenantInfo.TryMatch(true);
            }
            else
            {
                //Console.WriteLine($"\t\t tenantKey not matched: {tenantKey}");
                requestTenantInfo.Name = SiteConfig.TENANT_DEFAULT_NAME;
                requestTenantInfo.TryMatch(tenantKey == null || false);
            }

            return requestTenantInfo;
        }

        /// <summary>
        ///Create tenant information
        /// </summary>
        /// <param name="createOrUpdate_TenantInfoDto"></param>
        /// <param name="throwIfExisted">Throws exception if Name or TenantKey already exists</param>
        /// <exception cref="NcfTenantException">Name or TenantKey already exists</exception>
        /// <returns></returns>
        public async Task<TenantInfoDto> CreateOrUpdateTenantInfoAsync(CreateOrUpdate_TenantInfoDto createOrUpdate_TenantInfoDto, bool throwIfExisted = false)
        {
            createOrUpdate_TenantInfoDto.TenantKey = createOrUpdate_TenantInfoDto.TenantKey.ToUpper();
            var tenantInfo = await GetObjectAsync(z => z.Id != createOrUpdate_TenantInfoDto.Id && (z.Name == createOrUpdate_TenantInfoDto.Name || z.TenantKey == createOrUpdate_TenantInfoDto.TenantKey));
            if (tenantInfo != null)
            {
                if (throwIfExisted)
                {
                    throw new NcfTenantException($"已存在名为 {createOrUpdate_TenantInfoDto.Name} 或 关键条件为 {createOrUpdate_TenantInfoDto.TenantKey} 的租户记录！");
                }
                else
                {
                    return base.Mapper.Map<TenantInfoDto>(tenantInfo);
                }
            }

            if (createOrUpdate_TenantInfoDto.Id > 0)
            {
                //edit
                tenantInfo = await GetObjectAsync(z => z.Id == createOrUpdate_TenantInfoDto.Id, null);
                if (tenantInfo == null)
                {
                    throw new NcfTenantException($"租户信息不存在！Id：{createOrUpdate_TenantInfoDto.Id}");
                }
                tenantInfo.Update(createOrUpdate_TenantInfoDto);
            }
            else
            {
                //New
                tenantInfo = new TenantInfo(createOrUpdate_TenantInfoDto.Name, createOrUpdate_TenantInfoDto.Enable, createOrUpdate_TenantInfoDto.TenantKey);
            }
            await SaveObjectAsync(tenantInfo);
            await tenantInfo.ClearCache(_serviceProvider);//For all modifications involving tenant information, clear the tenant information and update again.

            return base.Mapper.Map<TenantInfoDto>(tenantInfo);
        }

        /// <summary>
        /// Create default TenantInfo information
        /// </summary>
        /// <param name="httpContext">If null, automatically obtained from IHttpContextAccessor</param>
        /// <returns></returns>
        public async Task<TenantInfoDto> CreateInitTenantInfoAsync(HttpContext httpContext)
        {
            httpContext = httpContext ?? _httpContextAccessor.Value.HttpContext;
            var urlData = httpContext.Request;
            var host = urlData.Host.Host;
            CreateOrUpdate_TenantInfoDto createOrUpdate_TenantInfoDto = new CreateOrUpdate_TenantInfoDto(0, host, host, true, "系统初始化自动创建");
            return await CreateOrUpdateTenantInfoAsync(createOrUpdate_TenantInfoDto);
        }

        /// <summary>
        /// Does the spread Name exist?
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<bool> CheckNameExisted(int id, string name)
        {
            return await GetObjectAsync(z => z.Id != id && z.Name == name, null) != null;
        }

        /// <summary>
        /// Does the spread TenantKey exist?
        /// </summary>
        /// <param name="id"></param>
        /// <param name="tenantKey"></param>
        /// <returns></returns>
        public async Task<bool> CheckTenantKeyExisted(int id, string tenantKey)
        {
            tenantKey = tenantKey.ToUpper();
            return await GetObjectAsync(z => z.Id != id && z.TenantKey == tenantKey, null) != null;
        }

        public override async Task SaveChangesAsync()
        {

            await base.SaveChangesAsync();
        }

        /// <summary>
        /// Get RequestTenantInfo
        /// </summary>
        /// <param name="tenantInfo"></param>
        /// <returns></returns>
        public RequestTenantInfo GetRequestTenantInfo(TenantInfo tenantInfo)
        {
            var requestTenantInfo = new RequestTenantInfo()
            {
                Id = tenantInfo.Id,
                Name = tenantInfo.Name,
                TenantKey = tenantInfo.TenantKey
            };
            requestTenantInfo.TryMatch(true);
            return requestTenantInfo;
        }

        /// <summary>
        /// Force tenant information to be set
        /// </summary>
        /// <param name="requestTenantInfo"></param>
        /// <returns></returns>
        public bool SetTenantInfo(TenantInfo tenantInfo)
        {
            var requestTenantInfo = this.GetRequestTenantInfo(tenantInfo);
            return base.SetTenantInfo(requestTenantInfo);
        }

    }
}
