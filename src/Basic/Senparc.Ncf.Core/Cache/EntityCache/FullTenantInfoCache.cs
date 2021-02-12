using AutoMapper;
using Microsoft.EntityFrameworkCore;
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
    [AutoDIType(DILifecycleType.Transient)]
    public class FullTenantInfoCache : BaseCache<Dictionary<string, TenantInfoDto>>
    {
        public const string CACHE_KEY = "FullTenantInfoCache";
        private INcfDbData _dataContext => base._db as INcfDbData;

        private SenparcEntitiesBase _senparcEntitiesBase => _dataContext.BaseDataContext as SenparcEntitiesBase;

        private IMapper _mapper;

        public FullTenantInfoCache(INcfDbData db, IMapper mapper)
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
            var fullList = _senparcEntitiesBase.TenantInfos.Where(z => z.Enable).ToList();
            var tenantInfoCollection = new Dictionary<string, TenantInfoDto>(StringComparer.OrdinalIgnoreCase);
            fullList.ForEach(z => tenantInfoCollection[z.TenantKey.ToUpper()/*全部大写*/] = _mapper.Map<TenantInfoDto>(z));
            base.SetData(tenantInfoCollection, base.TimeOut, null);
            return base.Data;
        }
    }
}
