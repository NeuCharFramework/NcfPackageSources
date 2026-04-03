using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Senparc.CO2NET;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.DI;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.DatabaseModel;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.Tenant.Domain.Cache
{
    /* Need attention and optimization:
     * 1. The current dependency injected object INcfDbData will still be injected into SenparcEntitiesBase.
     * Once OnModelCreating() of SenparcEntitiesBase is executed, it will try to read the cache here. If the cache is not initialized, it will fall into an infinite loop.
     * Therefore, calling queries under INcfDbData is prohibited in the current cache.
     * 2. Avoid using INcfDbData, or avoid using BaseCache (because the default injection object of INcfDbData is SenparcEntities in the NCF template, inherited from SenparcEntitiesBase)
     */

    //TODO: Temporarily added tenants also need to be added to the cache.

    /// <summary>
    ///Multi-tenant information cache
    /// </summary>
    [AutoDIType(DILifecycleType.Scoped)]
    public class FullTenantInfoCache : BaseCache<Dictionary<string, TenantInfoDto>>
    {
        public const string CACHE_KEY = "FullTenantInfoCache";


        private IMapper _mapper;
        private SenparcEntitiesMultiTenant _senparcEntitiesMultiTenant => base._db.BaseDataContext as SenparcEntitiesMultiTenant;
        //private SenparcEntitiesBase _senparcEntitiesBase => base._db.BaseDataContext as SenparcEntitiesBase;

        /// <summary>
        /// Get the currently enabled TenantInfo
        /// </summary>
        /// <returns></returns>
        private async Task<List<TenantInfo>> GetEnabledListAsync()
        {
            List<TenantInfo> list = null;
            try
            {
                _senparcEntitiesMultiTenant.SetMultiTenantEnable(false);//TODO: There is a thread safety issue here. TenantInfos does not have multi-tenant properties and can be queried directly.
                list = await _senparcEntitiesMultiTenant.Set<TenantInfo>().Where(z => z.Enable).ToListAsync();
            }
            finally
            {
                _senparcEntitiesMultiTenant.ResetMultiTenantEnable();
            }
            return list;
        }


        public FullTenantInfoCache(TenantInfoDbData db, IMapper mapper, SenparcEntitiesMultiTenant senparcEntitiesMultiTenantBase)
            : this(CACHE_KEY, db, 1440)
        {
            _mapper = mapper;
        }

        public FullTenantInfoCache(string CACHE_KEY, INcfDbData db, int timeOut) : base(CACHE_KEY, db)
        {
            base.TimeOut = timeOut;
        }

        public override Dictionary<string, TenantInfoDto> Update()
        {
            throw new Senparc.Ncf.Core.Exceptions.NcfTenantException("FullTenantInfoCache 不支持同步更新方法，请使用异步方法 UpdateAsync()");
        }

        public override async Task<Dictionary<string, TenantInfoDto>> UpdateAsync()
        {
            List<TenantInfo> fullList = null;

            try
            {
                fullList = await GetEnabledListAsync();
            }
            catch (Exception ex)
            {
                //It may be currently in the process of obtaining Middleware, and multi-tenant acquisition has not yet been completed.
                //If upgrading from an older version, the table does not exist and needs to be updated

                //TODO:_senparcEntitiesMultiTenant is currently not doing Migration, so it cannot actually be updated.
                _senparcEntitiesMultiTenant.ResetMigrate();
                _senparcEntitiesMultiTenant.Migrate();

                SenparcTrace.SendCustomLog($"{nameof(FullTenantInfoCache)} - {nameof(UpdateAsync)} 异常", ex.ToString());

                //var HttpContextAccessor = _serviceProvider.GetRequiredService<IHttpContextAccessor>();//Note: TenantInfoService.SetScopedRequestTenantInfoAsync() cannot be used here, otherwise it will cause an infinite loop
            }
            finally
            {
                fullList = fullList ?? await GetEnabledListAsync();
            }

            var tenantInfoCollection = new Dictionary<string, TenantInfoDto>(StringComparer.OrdinalIgnoreCase);
            fullList.ForEach(z => tenantInfoCollection[z.TenantKey.ToUpper()/*all caps*/] = _mapper.Map<TenantInfoDto>(z));
            await base.SetDataAsync(tenantInfoCollection, base.TimeOut, null);

            return await GetDataAsync();
        }
    }
}
