using System;

namespace Senparc.Ncf.Core.Cache
{
    public interface ICacheData
    {
        /// <summary>
        /// 缓存键
        /// </summary>
        string Key { get; }

        DateTime CacheTime { get; set; }
    }
}
