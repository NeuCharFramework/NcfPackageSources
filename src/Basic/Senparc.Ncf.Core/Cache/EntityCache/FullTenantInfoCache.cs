using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.Ncf.Core.DI;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Cache
{
    [AutoDIType(DILifecycleType.Scoped)]
    public class FullTenantInfoCache : BaseCache<Dictionary<string, TenantInfoDto>>
    {
        public const string CACHE_KEY = "FullTenantInfoCache";

        private SenparcEntitiesBase _senparcEntitiesBase => base._db.BaseDataContext as SenparcEntitiesBase;

        private IMapper _mapper;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 获取当前启用状态的 TenantInfo
        /// </summary>
        /// <param name="scopedSenparcEntitiesBase"></param>
        /// <returns></returns>
        private async Task<List<TenantInfo>> GetEnabledListAsync(SenparcEntitiesBase scopedSenparcEntitiesBase)
        {
            List<TenantInfo> list = null;
            try
            {
                scopedSenparcEntitiesBase.SetMultiTenantEnable(false);
                list = await scopedSenparcEntitiesBase.TenantInfos.Where(z => z.Enable).ToListAsync();
            }
            finally
            {
                scopedSenparcEntitiesBase.ResetMultiTenantEnable();
            }
            return list;
        }


        public FullTenantInfoCache(INcfDbData db, IMapper mapper, IServiceProvider serviceProvider)
            : this(CACHE_KEY, db, 1440)
        {
            _mapper = mapper;
            this._serviceProvider = serviceProvider;
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

            using (var scope = _serviceProvider.CreateScope())
            {
                SenparcEntitiesBase scopedSenparcEntitiesBase = scope.ServiceProvider.GetRequiredService<SenparcEntitiesBase>();
                Console.WriteLine($"senparcEntitiesBase:{_senparcEntitiesBase.GetHashCode()}");
                Console.WriteLine($"scopedSenparcEntitiesBase:{scopedSenparcEntitiesBase.GetHashCode()}");

                try
                {
                    fullList = await GetEnabledListAsync(scopedSenparcEntitiesBase);
                }
                catch (Exception)
                {
                    //当前可能正在 Middleware 的获取过程中，还没有完成多租户获取
                    //如果从旧版本升级，表不存在，则需要更新
                    scopedSenparcEntitiesBase.ResetMigrate();
                    scopedSenparcEntitiesBase.Migrate();

                    //var HttpContextAccessor = _serviceProvider.GetRequiredService<IHttpContextAccessor>();//注意：这里不能使用 TenantInfoService.SetScopedRequestTenantInfoAsync()， 否则会引发死循环
                }
                finally
                {
                    fullList = fullList ?? await GetEnabledListAsync(scopedSenparcEntitiesBase);
                }
            }

            var tenantInfoCollection = new Dictionary<string, TenantInfoDto>(StringComparer.OrdinalIgnoreCase);
            fullList.ForEach(z => tenantInfoCollection[z.TenantKey.ToUpper()/*全部大写*/] = _mapper.Map<TenantInfoDto>(z));
            await base.SetDataAsync(tenantInfoCollection, base.TimeOut, null);

            return await GetDataAsync();
        }
    }
}
