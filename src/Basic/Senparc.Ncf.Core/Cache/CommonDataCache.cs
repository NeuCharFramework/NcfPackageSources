using System;

namespace Senparc.Ncf.Core.Cache
{
    public class CommonDataCache<T> : BaseCache<T>, ICommonDataCache<T> where T : class,new()
    {
        public const string CACHE_KEY = "__CommonDataCache";
        string _key;
        Func<T> _fun;

        /// <summary>
        ///Public cache module, default timeout: 1440 minutes (1 day)
        /// </summary>
        /// <param name="key">Globally unique Key (only needs to be unique within the CommonDataCache module)</param>
        /// <param name="fun"></param>
        public CommonDataCache(string key, Func<T> fun)
            :this(key, 1440, fun)
        {
        }

        /// <summary>
        ///Public cache module
        /// </summary>
        /// <param name="key">Globally unique Key (only needs to be unique within the CommonDataCache module)</param>
        /// <param name="timeout">Cache time (minutes)</param>
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
