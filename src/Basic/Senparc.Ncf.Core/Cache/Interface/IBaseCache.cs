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
        /// Data不能在Update()方法中调用，否则会引发循环调用。Update()方法中应该使用SetData()方法
        /// </summary>
        DateTime CacheTime { get; set; }
        DateTime CacheTimeOut { get; set; }

        #region 同步方法
        T Data { get; set; }
        void RemoveCache();
        void SetData(T value, int timeOut, BaseCache<T>.UpdateWithBataBase updateWithDatabases);
        T Update();
        void UpdateToDatabase(T obj);
        #endregion

        #region 异步方法
        Task<T> GetDataAsync();
        Task RemoveCacheAsync();
        Task SetDataAsync(T value, int timeOut, BaseCache<T>.UpdateWithBataBase updateWithDatabases);
        Task<T> UpdateAsync();
        Task UpdateToDatabaseAsync(T obj);
        #endregion


        ///// <summary>
        ///// 更新到缓存
        ///// </summary>
        ///// <param name="key"></param>
        ///// <param name="obj"></param>
        ///// <returns></returns>
        //bool UpdateToCache(string key, T obj);
    }
}
