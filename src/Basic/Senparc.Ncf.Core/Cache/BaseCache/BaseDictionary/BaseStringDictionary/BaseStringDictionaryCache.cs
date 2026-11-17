/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BaseStringDictionaryCache.cs
    File description: BaseStringDictionaryCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;

namespace Senparc.Ncf.Core.Cache
{
    public interface IBaseStringDictionaryCache<TValue> : IBaseStringDictionaryCache<TValue, TValue> where TValue : class,new()
    {
    }
    public interface IBaseStringDictionaryCache<TValue, TEntity> : IBaseDictionaryCache<string, TValue, TEntity> where TValue : class,new()
    {

    }

    public abstract class BaseStringDictionaryCache<TValue> : BaseStringDictionaryCache<TValue, TValue>,
                                                                       IBaseStringDictionaryCache<TValue>
        where TValue : class, new()
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="CACHE_KEY"></param>
        /// <param name="db"></param>
        ///Legacy comment translated to English.
        public BaseStringDictionaryCache(string CACHE_KEY, INcfDbData db, int timeOut)
            : base(CACHE_KEY, db, timeOut)
        {
            base.TimeOut = timeOut;
        }
    }

    public abstract class BaseStringDictionaryCache<TValue, TEntity> : BaseDictionaryCache<string, TValue, TEntity>, IBaseStringDictionaryCache<TValue, TEntity> 
        where TValue : class,new()
        where TEntity : class/*, new()*/
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="CACHE_KEY"></param>
        /// <param name="db"></param>
        ///Legacy comment translated to English.
        public BaseStringDictionaryCache(string CACHE_KEY, INcfDbData db, int timeOut)
            : base(CACHE_KEY, db, timeOut)
        {
            base.TimeOut = timeOut;
        }
    }
}
