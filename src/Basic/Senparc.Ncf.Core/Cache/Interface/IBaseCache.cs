/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: IBaseCache.cs

    File description: IBaseCache related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Threading.Tasks;
using Senparc.CO2NET.Cache;
using Senparc.Ncf.Core.DI;

namespace Senparc.Ncf.Core.Cache
{
    public interface IBaseCache<T> : IAutoDI
       where T : class, new()
    {
        IBaseObjectCacheStrategy Cache { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        DateTime CacheTime { get; set; }
        DateTime CacheTimeOut { get; set; }

        #region Legacy section
        T Data { get; set; }
        void RemoveCache();
        void SetData(T value, int timeOut, BaseCache<T>.UpdateWithBataBase updateWithDatabases);
        T Update();
        void UpdateToDatabase(T obj);
        #endregion

        #region Legacy section
        Task<T> GetDataAsync();
        Task RemoveCacheAsync();
        Task SetDataAsync(T value, int timeOut, BaseCache<T>.UpdateWithBataBase updateWithDatabases);
        Task<T> UpdateAsync();
        Task UpdateToDatabaseAsync(T obj);
        #endregion


        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        ///// <param name="key"></param>
        ///// <param name="obj"></param>
        ///// <returns></returns>
        //bool UpdateToCache(string key, T obj);
    }
}
