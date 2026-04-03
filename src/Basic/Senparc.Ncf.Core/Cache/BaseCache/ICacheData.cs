using System;

namespace Senparc.Ncf.Core.Cache
{
    public interface ICacheData
    {
        /// <summary>
        ///cache key
        /// </summary>
        string Key { get; }

        DateTime CacheTime { get; set; }
    }
}
