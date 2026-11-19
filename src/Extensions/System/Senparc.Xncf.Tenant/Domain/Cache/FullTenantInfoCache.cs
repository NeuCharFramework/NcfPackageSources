/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FullTenantInfoCache.cs
    File description: FullTenantInfoCache related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261001
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.15.14 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.15.17 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using AutoMapper;
using Senparc.CO2NET;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.DI;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.DatabaseModel;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.Models;
using Senparc.Xncf.Tenant.ACL.Repository;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.Tenant.Domain.Cache
{
    /* Legacy comment translated to English.

     * Legacy comment translated to English.

     *  Legacy comment translated to English.

     *  Legacy comment translated to English.

     * Legacy comment translated to English.

     */

    //Legacy comment translated to English.

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [AutoDIType(DILifecycleType.Scoped)]
    public class FullTenantInfoCache : BaseCache<Dictionary<string, TenantInfoDto>>
    {
        public const string CACHE_KEY = "FullTenantInfoCache";


        private IMapper _mapper;
        private readonly IRepositoryBase<TenantInfo> _tenantInfoRepository;
        private SenparcEntitiesMultiTenant _senparcEntitiesMultiTenant => base._db.BaseDataContext as SenparcEntitiesMultiTenant;
        //private SenparcEntitiesBase _senparcEntitiesBase => base._db.BaseDataContext as SenparcEntitiesBase;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        private async Task<List<TenantInfo>> GetEnabledListAsync()
        {
            return await _tenantInfoRepository.GetObjectListAsync(
                z => z.Enable, z => z.Id, OrderingType.Ascending, 0, 0).ConfigureAwait(false);
        }


        public FullTenantInfoCache(TenantInfoDbData db, IMapper mapper, SenparcEntitiesMultiTenant senparcEntitiesMultiTenantBase,
            TenantInfoRepository tenantInfoRepository)
            : this(CACHE_KEY, db, 1440, tenantInfoRepository)
        {
            _mapper = mapper;
        }

        public FullTenantInfoCache(TenantInfoDbData db, IMapper mapper, SenparcEntitiesMultiTenant senparcEntitiesMultiTenantBase)
            : this(db, mapper, senparcEntitiesMultiTenantBase, new TenantInfoRepository(db))
        {
        }

        public FullTenantInfoCache(string CACHE_KEY, INcfDbData db, int timeOut)
            : this(CACHE_KEY, db, timeOut, new ClientRepositoryBase<TenantInfo>(db))
        {
        }

        private FullTenantInfoCache(string CACHE_KEY, INcfDbData db, int timeOut, IRepositoryBase<TenantInfo> tenantInfoRepository)
            : base(CACHE_KEY, db)
        {
            _tenantInfoRepository = tenantInfoRepository;
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
                //Legacy comment translated to English.
                //Legacy comment translated to English.

                //Legacy comment translated to English.
                _senparcEntitiesMultiTenant.ResetMigrate();
                _senparcEntitiesMultiTenant.Migrate();

                SenparcTrace.SendCustomLog($"{nameof(FullTenantInfoCache)} - {nameof(UpdateAsync)} 异常", ex.ToString());

                //Legacy comment translated to English.
            }
            finally
            {
                fullList = fullList ?? await GetEnabledListAsync();
            }

            var tenantInfoCollection = new Dictionary<string, TenantInfoDto>(StringComparer.OrdinalIgnoreCase);
            fullList.ForEach(z => tenantInfoCollection[z.TenantKey.ToUpper()/*Legacy comment translated to English.*/] = _mapper.Map<TenantInfoDto>(z));
            await base.SetDataAsync(tenantInfoCollection, base.TimeOut, null);

            return await GetDataAsync();
        }
    }
}
