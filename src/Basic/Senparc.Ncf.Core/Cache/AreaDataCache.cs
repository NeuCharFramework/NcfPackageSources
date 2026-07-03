/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AreaDataCache.cs
    文件功能描述：AreaDataCache 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System.Collections.Generic;
using Senparc.CO2NET;
using Senparc.Ncf.Core.DI;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;


namespace Senparc.Ncf.Core.Cache
{
    /// <summary>
    /// 省数据缓存（来自XML文件中）。不要直接使用此方法，使用Common.AreaData获取。
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
    /// 市数据缓存（来自XML文件中）。不要直接使用此方法，使用Common.AreaData获取。
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
    /// 区县数据缓存（来自XML文件中）。不要直接使用此方法，使用Common.AreaData获取。
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
