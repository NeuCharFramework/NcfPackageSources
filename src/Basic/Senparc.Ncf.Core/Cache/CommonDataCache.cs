/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: CommonDataCache.cs
    File description: CommonDataCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

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
        /// Initializes a cache using the default timeout.
        /// </summary>
        /// <param name="key">The logical cache key suffix.</param>
        /// <param name="fun">The factory used to refresh the value.</param>
        public CommonDataCache(string key, Func<T> fun)
            :this(key, 1440, fun)
        {
        }

        /// <summary>
        /// Initializes a cache with an explicit timeout and refresh factory.
        /// </summary>
        /// <param name="key">The logical cache key suffix.</param>
        /// <param name="timeout">The cache timeout in minutes.</param>
        /// <param name="fun">The factory used to refresh the value.</param>
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
