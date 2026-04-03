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
        ///Data cannot be called in the Update() method, otherwise it will cause a loop call. SetData() method should be used in Update() method
        /// </summary>
        DateTime CacheTime { get; set; }
        DateTime CacheTimeOut { get; set; }

        #region Synchronous Methods
        T Data { get; set; }
        void RemoveCache();
        void SetData(T value, int timeOut, BaseCache<T>.UpdateWithBataBase updateWithDatabases);
        T Update();
        void UpdateToDatabase(T obj);
        #endregion

        #region Asynchronous Methods
        Task<T> GetDataAsync();
        Task RemoveCacheAsync();
        Task SetDataAsync(T value, int timeOut, BaseCache<T>.UpdateWithBataBase updateWithDatabases);
        Task<T> UpdateAsync();
        Task UpdateToDatabaseAsync(T obj);
        #endregion


        ///// <summary>
        ///// Update to cache
        ///// </summary>
        ///// <param name="key"></param>
        ///// <param name="obj"></param>
        ///// <returns></returns>
        //bool UpdateToCache(string key, T obj);
    }
}
