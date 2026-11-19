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
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        /// <param name="fun"></param>
        public CommonDataCache(string key, Func<T> fun)
            :this(key, 1440, fun)
        {
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
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
