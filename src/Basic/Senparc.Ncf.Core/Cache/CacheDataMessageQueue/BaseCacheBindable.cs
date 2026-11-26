/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BaseCacheBindable.cs
    File description: BaseCacheBindable related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Runtime.CompilerServices;
using Senparc.CO2NET.Cache;
using Senparc.Ncf.Core.Entities;
using Senparc.Ncf.Log;

namespace Senparc.Ncf.Core.Cache
{
    /// <summary>
    /// Binds property changes on cache data objects to deferred cache-update actions.
    /// </summary>
    [Serializable]
    public abstract class BaseCacheBindable<T> : BindableBase where T : class, ICacheData, new()
    {
        // Cache data exposes its key through ICacheData; the former abstract Key
        // member is retained only in historical source.
        //public abstract object Key { get; }

        protected string GenerateKey_Name { get; set; }
        protected string GenerateKey_ActionName { get; set; }


        /// <summary>
        /// Handles cache-data property changes and schedules the configured deferred cache update.
        /// </summary>
        /// <param name="sender">The changed cache-data instance.</param>
        /// <param name="e">The property-change event data.</param>
        protected void BaseCacheBindable_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender == null)
            {
                LogUtility.Cache.ErrorFormat("BaseCacheBindable发生错误，不是BaseCacheBindable类型。当前参数类型：{0}", sender.GetType());
                return;
            }

            var objCacheData = sender as ICacheData;
            if (objCacheData == null)
            {
                LogUtility.Cache.ErrorFormat("BaseCacheBindable发生错误，没有实现ICacheData接口。当前参数类型：{0}", sender.GetType());
                return;
            }

            if (objCacheData.Key == null)
            {
                LogUtility.Cache.ErrorFormat("BaseCacheBindable发生错误，Key为空。当前参数类型：{0}", sender.GetType());
                return;
            }

            var mqKey = CacheDataMessageQueue.GenerateKey("SenparcCache", sender.GetType(), objCacheData.Key as string, "UpdateCache");

            // Coalesce property changes through the cache message queue so repeated
            // updates do not perform immediate synchronous writes.
            CacheDataMessageQueue mq = new CacheDataMessageQueue();
            mq.Add(mqKey, () =>
            {
                var cacheStragegy = CacheStrategyFactory.GetObjectCacheStrategyInstance();
                //var cacheKey = objCacheData.Key;
                objCacheData.CacheTime = DateTime.Now;
                cacheStragegy.Set(objCacheData.Key, objCacheData as T);

                //var cacheKey = ContainerHelper.GetCacheKey(this.GetType());
                //containerCacheStragegy.UpdateContainerBag(cacheKey, containerBag);
            });
        }


        /// <summary>
        /// Sets a backing field and exposes the standard property-change notification.
        /// </summary>
        /// <typeparam name="T">The cache data type.</typeparam>
        /// <param name="storage">The backing field.</param>
        /// <param name="value">The new value.</param>
        /// <param name="propertyName">The caller property name.</param>
        /// <returns><see langword="true"/> when the value changed.</returns>
        protected bool SetContainerProperty(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            var result = base.SetProperty(ref storage, value, propertyName);
            return result;
        }

        /// <summary>
        /// Initializes cache binding and subscribes to property changes.
        /// </summary>
        public BaseCacheBindable()
        {
            base.PropertyChanged += BaseCacheBindable_PropertyChanged;
        }
    }
}
