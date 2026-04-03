using System.Collections.Generic;
using Senparc.CO2NET;
using Senparc.Ncf.Core.DI;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;


namespace Senparc.Ncf.Core.Cache
{
    /// <summary>
    /// Provincial data cache (from XML file). Do not use this method directly, use Common.AreaData to get it.
    /// </summary>
    [AutoDIType(DILifecycleType.Singleton)]
    public class AreaDataCache_Province : BaseCache<List<AreaXML_Provinces>>
    {
        public const string CACHE_KEY = "AreaDataCache:Province";


        public AreaDataCache_Province()
            : base(CACHE_KEY, null)
        {
            base.TimeOut = 9999;
        }

        public override List<AreaXML_Provinces> Update()
        {
            return null;
        }
    }

    /// <summary>
    /// City data cache (from XML file). Do not use this method directly, use Common.AreaData to get it.
    /// </summary>
    public class AreaDataCache_City : BaseCache<List<AreaXML_Cities>>
    {
        public const string CACHE_KEY = "AreaDataCache:City";

        public AreaDataCache_City()
            : base(CACHE_KEY, null)
        {
            base.TimeOut = 9999;
        }

        public override List<AreaXML_Cities> Update()
        {
            return null;
        }
    }

    /// <summary>
    /// District and county data cache (from XML file). Do not use this method directly, use Common.AreaData to get it.
    /// </summary>
    public class AreaDataCache_District : BaseCache<List<AreaXML_Districts>>
    {
        public const string CACHE_KEY = "AreaDataCache:District";

        public AreaDataCache_District()
            : base(CACHE_KEY, null)
        {
            base.TimeOut = 9999;
        }

        public override List<AreaXML_Districts> Update()
        {
            return null;
        }
    }
}
