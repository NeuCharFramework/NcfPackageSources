/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AreaDataCache.cs
    File description: AreaDataCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System.Collections.Generic;
using Senparc.CO2NET;
using Senparc.Ncf.Core.DI;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;


namespace Senparc.Ncf.Core.Cache
{
    /// <summary>Singleton cache for province area records.</summary>
    [AutoDIType(DILifecycleType.Singleton)]
    public class AreaDataCache_Province : BaseCache<List<AreaXML_Provinces>>
    {
        public const string CACHE_KEY = "AreaDataCache:Province";


        /// <summary>Initializes the province cache with the module cache key.</summary>
        public AreaDataCache_Province()
            : base(CACHE_KEY, null)
        {
            base.TimeOut = 9999;
        }

        /// <inheritdoc />
        public override List<AreaXML_Provinces> Update()
        {
            return null;
        }
    }

    /// <summary>Cache for city area records.</summary>
    public class AreaDataCache_City : BaseCache<List<AreaXML_Cities>>
    {
        public const string CACHE_KEY = "AreaDataCache:City";

        /// <summary>Initializes the city cache with the module cache key.</summary>
        public AreaDataCache_City()
            : base(CACHE_KEY, null)
        {
            base.TimeOut = 9999;
        }

        /// <inheritdoc />
        public override List<AreaXML_Cities> Update()
        {
            return null;
        }
    }

    /// <summary>Cache for district area records.</summary>
    public class AreaDataCache_District : BaseCache<List<AreaXML_Districts>>
    {
        public const string CACHE_KEY = "AreaDataCache:District";

        /// <summary>Initializes the district cache with the module cache key.</summary>
        public AreaDataCache_District()
            : base(CACHE_KEY, null)
        {
            base.TimeOut = 9999;
        }

        /// <inheritdoc />
        public override List<AreaXML_Districts> Update()
        {
            return null;
        }
    }
}
