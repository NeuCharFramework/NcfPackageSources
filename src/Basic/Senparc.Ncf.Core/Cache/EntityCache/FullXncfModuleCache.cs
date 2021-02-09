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
            var xncfModule = await (_dataContext.BaseDataContext as SenparcEntitiesBase).XncfModules
                                        .FirstOrDefaultAsync(z => z.Name == key)
                                        .ConfigureAwait(false);
            var fullXncfModule = await base.InsertObjectToCacheAsync(key, xncfModule).ConfigureAwait(false);
            return fullXncfModule;
        }
    }
}
