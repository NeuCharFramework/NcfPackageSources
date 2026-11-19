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
    ///Legacy comment translated to English.
    /// </summary>
    [Serializable]
    public abstract class BaseCacheBindable<T> : BindableBase where T : class, ICacheData, new()
    {
        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        //public abstract object Key { get; }

        protected string GenerateKey_Name { get; set; }
        protected string GenerateKey_ActionName { get; set; }


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

            //Legacy comment translated to English.

            //Legacy comment translated to English.
            CacheDataMessageQueue mq = new CacheDataMessageQueue();
            mq.Add(mqKey, () =>
            {
                var cacheStragegy = CacheStrategyFactory.GetObjectCacheStrategyInstance();
                //var cacheKey = objCacheData.Key;
                objCacheData.CacheTime = DateTime.Now;//Legacy comment translated to English.
                cacheStragegy.Set(objCacheData.Key, objCacheData as T);

                //var cacheKey = ContainerHelper.GetCacheKey(this.GetType());
                //Legacy comment translated to English.
                //containerCacheStragegy.UpdateContainerBag(cacheKey, containerBag);
            });
        }


        /// <summary>
        /// settingsContainerproperty
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storage"></param>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected bool SetContainerProperty(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            var result = base.SetProperty(ref storage, value, propertyName);
            return result;
        }

        public BaseCacheBindable()
        {
            base.PropertyChanged += BaseCacheBindable_PropertyChanged;
        }
    }
}
