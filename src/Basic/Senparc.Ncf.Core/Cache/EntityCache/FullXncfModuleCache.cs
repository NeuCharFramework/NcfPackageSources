/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FullXncfModuleCache.cs
    文件功能描述：FullXncfModuleCache 相关实现
    
    
    创建标识：Senparc - 20200916
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.CO2NET;
using Senparc.Ncf.Core.DI;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Cache
{
    [AutoDIType(DILifecycleType.Scoped)]
    public class FullXncfModuleCache : BaseStringDictionaryCache<FullXncfModule, XncfModule>
    {
        public const string CACHE_KEY = "FullXncfModuleCache";
        private INcfDbData _dataContext => base._db as INcfDbData;

        public FullXncfModuleCache(INcfDbData db)
            : this(CACHE_KEY, db, 1440)
        {
        }

        public FullXncfModuleCache(string CACHE_KEY, INcfDbData db, int timeOut) : base(CACHE_KEY, db, timeOut)
        {
        }

        [Obsolete]
        public override FullXncfModule InsertObjectToCache(string key)
        {
            throw new NotImplementedException();
        }

        public override async Task<FullXncfModule> InsertObjectToCacheAsync(string key)
        {
            var xncfModule = await (_dataContext.BaseDataContext as SenparcEntitiesBase).Set<XncfModule>()
                                        .FirstOrDefaultAsync(z => z.Name == key)
                                        .ConfigureAwait(false);
            var fullXncfModule = await base.InsertObjectToCacheAsync(key, xncfModule).ConfigureAwait(false);
            return fullXncfModule;
        }
    }
}
