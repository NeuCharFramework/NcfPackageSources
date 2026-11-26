/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BaseStringDictionaryCache.cs
    File description: BaseStringDictionaryCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

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
        /// Initializes a string-keyed cache backed by the supplied database wrapper.
        /// </summary>
        /// <param name="CACHE_KEY">The cache key prefix.</param>
        /// <param name="db">The database wrapper used for persistence.</param>
        /// <param name="timeOut">The cache timeout.</param>
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
        /// Initializes a string-keyed cache with a distinct entity persistence type.
        /// </summary>
        /// <param name="CACHE_KEY">The cache key prefix.</param>
        /// <param name="db">The database wrapper used for persistence.</param>
        /// <param name="timeOut">The cache timeout.</param>
        public BaseStringDictionaryCache(string CACHE_KEY, INcfDbData db, int timeOut)
            : base(CACHE_KEY, db, timeOut)
        {
            base.TimeOut = timeOut;
        }
    }
}
