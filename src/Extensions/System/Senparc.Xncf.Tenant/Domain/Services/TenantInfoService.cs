/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TenantInfoService.cs
    File description: TenantInfoService related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261103
    Change description: v0.15.14 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.15.17 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

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

        /// <summary>
        /// Initializes tenant persistence and request-context resolution dependencies.
        /// </summary>
        /// <param name="repo">Tenant information repository.</param>
        /// <param name="serviceProvider">Dependency-injection provider.</param>
        /// <param name="httpContextAccessor">Lazy accessor for the current HTTP context.</param>
        public TenantInfoService(TenantInfoRepository repo, IServiceProvider serviceProvider, Lazy<IHttpContextAccessor> httpContextAccessor)
            : base(repo, serviceProvider)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Resolves the current request tenant according to the configured tenant rule and
        /// populates the scoped <see cref="RequestTenantInfo"/> instance.
        /// </summary>
        /// <param name="httpContext">The current HTTP context, or <see langword="null"/> to use the accessor.</param>
        /// <returns>The scoped tenant information, matched when the request identifies a known tenant.</returns>
        /// <exception cref="NcfUninstallException">Thrown when tenant cache loading fails during installation or multi-tenant setup.</exception>
        public async Task<RequestTenantInfo> SetScopedRequestTenantInfoAsync(HttpContext httpContext)
        {
            var requestTenantInfo = _serviceProvider.GetRequiredService<RequestTenantInfo>();
            httpContext = httpContext ?? _httpContextAccessor.Value.HttpContext;
            string tenantKey = null;
            //string[] tanantCookieInfo = null;
            switch (SiteConfig.SenparcCoreSetting.TenantRule)
            {
                case TenantRule.DomainName:
                    //Legacy comment translated to English.
                    var urlData = httpContext.Request;
                    var host = urlData.Host.Host.ToUpper()/*Legacy comment translated to English.*/;//Legacy comment translated to English.
                    tenantKey = host;
                    break;
                case TenantRule.RequestHeader:
                    var headerTenantKey = httpContext.Request.Headers["TenantKey"];
                    if (!string.IsNullOrEmpty(headerTenantKey))
                    {
                        tenantKey = headerTenantKey[0].ToUpper();
                    }
                    else if (httpContext.User?.Identity?.IsAuthenticated == true)
                    {
                        tenantKey = httpContext.User.FindFirst("TenantKey")?.Value?.ToUpper();
                    }
                    break;
                case TenantRule.LoginInput:
                    {
                        //Legacy comment translated to English.
                        var tenantKeyClaim = httpContext.User?.Claims.FirstOrDefault(c => c.Type == "TenantKey");

                        if (tenantKeyClaim == null)
                        {
                            tenantKey = null;
                            break;
                        }
                        else
                        {
                            //Legacy comment translated to English.
                            tenantKey = tenantKeyClaim.Value;
                        }
                    }
                    break;
                default:
                    throw new Exception("未处理的 TenantRule 类型");
            }

            //Legacy comment translated to English.

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
                    //Legacy comment translated to English.
                    //tenantKey = null;
                    throw new NcfUninstallException("fullTenantInfoCache.GetDataAsync 读取失败，推测系统未安装或多租户未启用", ex);
                }

                //Legacy comment translated to English.
                //tenantKey = null;

                throw new NcfUninstallException("fullTenantInfoCache.GetDataAsync 读取失败，推测系统未安装", ex);
            }

            //Legacy comment translated to English.

            if (!string.IsNullOrEmpty(tenantKey) && tenantInfoCollection.TryGetValue(tenantKey, out var tenantInfoDto))
            {
                //Legacy comment translated to English.
                requestTenantInfo.Id = tenantInfoDto.Id;
                requestTenantInfo.Name = tenantInfoDto.Name;
                requestTenantInfo.TenantKey = tenantInfoDto.TenantKey;
                requestTenantInfo.TryMatch(true);
            }
            else
            {
                //Legacy comment translated to English.
                requestTenantInfo.Name = SiteConfig.TENANT_DEFAULT_NAME;
                requestTenantInfo.TryMatch(tenantKey == null || false);
            }

            return requestTenantInfo;
        }

        /// <summary>
        /// Creates or updates tenant metadata after normalizing the tenant key and checking duplicates.
        /// </summary>
        /// <param name="createOrUpdate_TenantInfoDto">The tenant create/update data.</param>
        /// <param name="throwIfExisted">Whether duplicate name/key conflicts should throw instead of returning the existing DTO.</param>
        /// <returns>The persisted tenant DTO.</returns>
        /// <exception cref="NcfTenantException">Thrown when a requested tenant does not exist or duplicate conflicts are configured to throw.</exception>
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
                //add
                tenantInfo = new TenantInfo(createOrUpdate_TenantInfoDto.Name, createOrUpdate_TenantInfoDto.Enable, createOrUpdate_TenantInfoDto.TenantKey);
            }
            await SaveObjectAsync(tenantInfo);
            await tenantInfo.ClearCache(_serviceProvider);//Legacy comment translated to English.

            return base.Mapper.Map<TenantInfoDto>(tenantInfo);
        }

        /// <summary>
        /// Creates initial tenant metadata using the current request host.
        /// </summary>
        /// <param name="httpContext">The current HTTP context, or <see langword="null"/> to use the accessor.</param>
        /// <returns>The created or existing initial tenant DTO.</returns>
        public async Task<TenantInfoDto> CreateInitTenantInfoAsync(HttpContext httpContext)
        {
            httpContext = httpContext ?? _httpContextAccessor.Value.HttpContext;
            var urlData = httpContext.Request;
            var host = urlData.Host.Host;
            CreateOrUpdate_TenantInfoDto createOrUpdate_TenantInfoDto = new CreateOrUpdate_TenantInfoDto(0, host, host, true, "系统初始化自动创建");
            return await CreateOrUpdateTenantInfoAsync(createOrUpdate_TenantInfoDto);
        }

        /// <summary>
        /// Checks whether another tenant uses the supplied display name.
        /// </summary>
        /// <param name="id">The tenant identifier to exclude.</param>
        /// <param name="name">The name to check.</param>
        /// <returns><see langword="true"/> when another tenant has the name.</returns>
        public async Task<bool> CheckNameExisted(int id, string name)
        {
            return await GetObjectAsync(z => z.Id != id && z.Name == name, null) != null;
        }

        /// <summary>
        /// Checks whether another tenant uses the supplied normalized tenant key.
        /// </summary>
        /// <param name="id">The tenant identifier to exclude.</param>
        /// <param name="tenantKey">The tenant key to check.</param>
        /// <returns><see langword="true"/> when another tenant has the key.</returns>
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
        /// Converts tenant persistence data into a matched request-tenant context.
        /// </summary>
        /// <param name="tenantInfo">The tenant information to convert.</param>
        /// <returns>A matched request tenant.</returns>
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
        /// Applies tenant information to the current multi-tenant database context.
        /// </summary>
        /// <param name="tenantInfo">The tenant information to apply.</param>
        /// <returns><see langword="true"/> when the base context accepts the tenant; otherwise, <see langword="false"/>.</returns>
        public bool SetTenantInfo(TenantInfo tenantInfo)
        {
            var requestTenantInfo = this.GetRequestTenantInfo(tenantInfo);
            return base.SetTenantInfo(requestTenantInfo);
        }

    }
}
