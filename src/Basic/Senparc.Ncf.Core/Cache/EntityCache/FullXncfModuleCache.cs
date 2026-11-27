/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FullXncfModuleCache.cs
    File description: FullXncfModuleCache related implementation

    Created by: Senparc - 20200916

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

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

        /// <summary>
        /// Initializes the default full-module cache using the standard cache key and timeout.
        /// </summary>
        /// <param name="db">The NCF database data provider.</param>
        public FullXncfModuleCache(INcfDbData db)
            : this(CACHE_KEY, db, 1440)
        {
        }

        /// <summary>
        /// Initializes a full-module cache with custom key and timeout settings.
        /// </summary>
        /// <param name="CACHE_KEY">The logical cache key.</param>
        /// <param name="db">The NCF database data provider.</param>
        /// <param name="timeOut">The cache timeout in minutes.</param>
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
