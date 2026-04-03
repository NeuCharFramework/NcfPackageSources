using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Cache.CsRedis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Cache.Extensions
{
    public static class ObjectCacheStrategyExtensions
    {
        public static IList<T> GetAllByPrefix<T>(this IBaseObjectCacheStrategy obj, string key)
        {
            if (obj is RedisObjectCacheStrategy)
            {
                RedisObjectCacheStrategy _obj = obj as RedisObjectCacheStrategy;
                return _obj.GetAllByPrefix<T>(key);
            }
            throw new Exception("Not implemented or cache does not support prefix access!");
        }

        public static async Task<IList<T>> GetAllByPrefixAsync<T>(this IBaseObjectCacheStrategy obj, string key)
        {
            if (obj is RedisObjectCacheStrategy)
            {
                var _obj = obj as RedisObjectCacheStrategy;
                return await _obj.GetAllByPrefixAsync<T>(key);
            }
            throw new Exception("Not implemented or cache does not support prefix access!");
        }
    }
}
