using System;
using System.Runtime.CompilerServices;
using Senparc.CO2NET.Cache;
using Senparc.Ncf.Core.Entities;
using Senparc.Ncf.Log;

namespace Senparc.Ncf.Core.Cache
{
    /// <summary>
    /// All entity base classes that require distributed caching
    /// </summary>
    [Serializable]
    public abstract class BaseCacheBindable<T> : BindableBase where T : class, ICacheData, new()
    {
        ///// <summary>
        ///// Cache key
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

            //Get the cache related information of the corresponding Container

            //Join the message queue and automatically update every period of time to prevent attributes from being edited continuously and update the cache repeatedly in a short period of time.
            CacheDataMessageQueue mq = new CacheDataMessageQueue();
            mq.Add(mqKey, () =>
            {
                var cacheStragegy = CacheStrategyFactory.GetObjectCacheStrategyInstance();
                //var cacheKey = objCacheData.Key;
                objCacheData.CacheTime = DateTime.Now;// Record cache time
                cacheStragegy.Set(objCacheData.Key, objCacheData as T);

                //var cacheKey = ContainerHelper.GetCacheKey(this.GetType());
                //containerBag.CacheTime = DateTime.Now;// Record cache time
                //containerCacheStragegy.UpdateContainerBag(cacheKey, containerBag);
            });
        }


        /// <summary>
        ///Set Container properties
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
