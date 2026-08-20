/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：CommonDataCache.cs
    文件功能描述：CommonDataCache 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache
{
    public class CommonDataCache<T> : BaseCache<T>, ICommonDataCache<T> where T : class,new()
    {
        public const string CACHE_KEY = "__CommonDataCache";
        string _key;
        Func<T> _fun;

        /// <summary>
        /// 公用缓存模块，默认超时时间：1440分钟（1天）
        /// </summary>
        /// <param name="key">全局唯一Key（只需要确保在CommonDataCache模块内唯一）</param>
        /// <param name="fun"></param>
        public CommonDataCache(string key, Func<T> fun)
            :this(key, 1440, fun)
        {
        }

        /// <summary>
        /// 公用缓存模块
        /// </summary>
        /// <param name="key">全局唯一Key（只需要确保在CommonDataCache模块内唯一）</param>
        /// <param name="timeout">缓存时间（分钟）</param>
        /// <param name="fun"></param>
        public CommonDataCache(string key, int timeout, Func<T> fun)
            : base(CACHE_KEY + key)
        {
            _key = CACHE_KEY + key;
            base.TimeOut = timeout;
            this._fun = fun;
        }

        public override T Update()
        {
            base.SetData(_fun(), base.TimeOut, null);
            return base.Data;
        }
    }
}
