using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core;
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

        public TenantInfoService(TenantInfoRepository repo, IServiceProvider serviceProvider, Lazy<IHttpContextAccessor> httpContextAccessor)
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
                    //Console.WriteLine("\t\t进入 SetScopedRequestTenantInfoAsync -> TenantRule.DomainName");
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

            //Console.WriteLine("\t\t准备进入 _serviceProvider.GetRequiredService<FullTenantInfoCache>()");

            var fullTenantInfoCache = _serviceProvider.GetRequiredService<FullTenantInfoCache>();
            Dictionary<string, TenantInfoDto> tenantInfoCollection;
            try
            {
                tenantInfoCollection = await fullTenantInfoCache.GetDataAsync();
            }
            catch
            {
                //数据库错误，通常为系统未安装
                throw new NcfUninstallException("fullTenantInfoCache.GetDataAsync 读取失败，推测系统未安装");
            }

            //Console.WriteLine($"\t\t已获取 tenantInfoCollection：{tenantInfoCollection.ToJson()}");

            if (!string.IsNullOrEmpty(tenantKey) && tenantInfoCollection.TryGetValue(tenantKey, out var tenantInfoDto))
            {
                //Console.WriteLine($"\t\t 匹配到 tenantKey：{tenantKey}");
                requestTenantInfo.Id = tenantInfoDto.Id;
                requestTenantInfo.Name = tenantInfoDto.Name;
                requestTenantInfo.TenantKey = tenantInfoDto.TenantKey;
                requestTenantInfo.TryMatch(true);
            }
            else
            {
                //Console.WriteLine($"\t\t 未匹配到 tenantKey：{tenantKey}");
                requestTenantInfo.Name = SiteConfig.TENANT_DEFAULT_NAME;
                requestTenantInfo.TryMatch(false);
            }

            return requestTenantInfo;
        }

        /// <summary>
        /// 创建租户信息
        /// </summary>
        /// <param name="createOrUpdate_TenantInfoDto"></param>
        /// <param name="throwIfExisted">如果 Name 或 TenantKey 已存在，则抛出异常</param>
        /// <exception cref="NcfTenantException">Name 或 TenantKey 已存在</exception>
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
                //编辑
                tenantInfo = await GetObjectAsync(z => z.Id == createOrUpdate_TenantInfoDto.Id, null);
                if (tenantInfo == null)
                {
                    throw new NcfTenantException($"租户信息不存在！Id：{createOrUpdate_TenantInfoDto.Id}");
                }
                tenantInfo.Update(createOrUpdate_TenantInfoDto);
            }
            else
            {
                //新增
                tenantInfo = new TenantInfo(createOrUpdate_TenantInfoDto.Name, createOrUpdate_TenantInfoDto.Enable, createOrUpdate_TenantInfoDto.TenantKey);
            }
            await SaveObjectAsync(tenantInfo);
            await tenantInfo.ClearCache(_serviceProvider);//所有涉及到租户信息的修改，都清除租户信息，重新更新

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
            CreateOrUpdate_TenantInfoDto createOrUpdate_TenantInfoDto = new CreateOrUpdate_TenantInfoDto(0, host, host, true, "系统初始化自动创建");
            return await CreateOrUpdateTenantInfoAsync(createOrUpdate_TenantInfoDto);
        }

        /// <summary>
        /// 价差 Name 是否存在
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<bool> CheckNameExisted(int id, string name)
        {
            return await GetObjectAsync(z => z.Id != id && z.Name == name, null) != null;
        }

        /// <summary>
        /// 价差 TenantKey 是否存在
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
    }
}
