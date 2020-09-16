using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.DI;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models.DataBaseModel;

namespace Senparc.Ncf.Core.Cache
{
    [AutoDIType(DILifecycleType.Scoped)]
    public class FullXncfModuleCache : BaseStringDictionaryCache<FullXncfModule, XncfModule>
    {
        public const string CACHE_KEY = "FullXncfModuleCache";
        private ISqlBaseFinanceData _dataContext => base._db as ISqlBaseFinanceData;

        public FullXncfModuleCache(ISqlBaseFinanceData db)
            : this(CACHE_KEY, db, 1440)
        {
        }

        public FullXncfModuleCache(string CACHE_KEY, ISqlBaseFinanceData db, int timeOut) : base(CACHE_KEY, db, timeOut)
        {
        }

        public override FullXncfModule InsertObjectToCache(string key)
        {
            var xncfModule = (_dataContext.BaseDataContext as SenparcEntitiesBase).XncfModules
                                .FirstOrDefault(z => z.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
            var fullXncfModule = base.InsertObjectToCache(key, xncfModule);
            return fullXncfModule;
        }
    }
}
