/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IBaseCache.cs
    File description: IBaseCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Threading.Tasks;
using Senparc.CO2NET.Cache;
using Senparc.Ncf.Core.DI;

namespace Senparc.Ncf.Core.Cache
{
    /// <summary>
    /// Defines the synchronous and asynchronous cache lifecycle for one data type.
    /// </summary>
    /// <typeparam name="T">The cached reference type.</typeparam>
    public interface IBaseCache<T> : IAutoDI
       where T : class, new()
    {
        /// <summary>
        /// Gets or sets the underlying cache strategy.
        /// </summary>
        IBaseObjectCacheStrategy Cache { get; set; }
        /// <summary>
        /// Gets or sets the time at which the current cache data was created.
        /// </summary>
        DateTime CacheTime { get; set; }
        /// <summary>
        /// Gets or sets the expiration time of the current cache data.
        /// </summary>
        DateTime CacheTimeOut { get; set; }

        #region Legacy section
        /// <summary>
        /// Gets or sets the current cached value.
        /// </summary>
        T Data { get; set; }
        /// <summary>
        /// Removes the cached value from the configured cache strategy.
        /// </summary>
        void RemoveCache();
        /// <summary>
        /// Stores a value and optionally persists it through the supplied callback.
        /// </summary>
        /// <param name="value">The value to cache.</param>
        /// <param name="timeOut">The cache timeout in seconds.</param>
        /// <param name="updateWithDatabases">Optional persistence callback.</param>
        void SetData(T value, int timeOut, BaseCache<T>.UpdateWithBataBase updateWithDatabases);
        /// <summary>
        /// Refreshes the value synchronously.
        /// </summary>
        /// <returns>The refreshed value.</returns>
        T Update();
        /// <summary>
        /// Persists a value to the backing database.
        /// </summary>
        /// <param name="obj">The value to persist.</param>
        void UpdateToDatabase(T obj);
        #endregion

        #region Legacy section
        /// <summary>
        /// Asynchronously gets the current cached value.
        /// </summary>
        Task<T> GetDataAsync();
        /// <summary>
        /// Asynchronously removes the cached value.
        /// </summary>
        Task RemoveCacheAsync();
        /// <summary>
        /// Asynchronously stores a value and optionally persists it.
        /// </summary>
        /// <param name="value">The value to cache.</param>
        /// <param name="timeOut">The cache timeout in seconds.</param>
        /// <param name="updateWithDatabases">Optional persistence callback.</param>
        Task SetDataAsync(T value, int timeOut, BaseCache<T>.UpdateWithBataBase updateWithDatabases);
        /// <summary>
        /// Asynchronously refreshes the value.
        /// </summary>
        /// <returns>A task containing the refreshed value.</returns>
        Task<T> UpdateAsync();
        /// <summary>
        /// Asynchronously persists a value to the backing database.
        /// </summary>
        /// <param name="obj">The value to persist.</param>
        Task UpdateToDatabaseAsync(T obj);
        #endregion


        // The former UpdateToCache(key, value) member was superseded by the
        // typed synchronous/asynchronous methods above.
    }
}
