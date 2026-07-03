/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ObjectCacheStrategyExtensions.cs
    文件功能描述：ObjectCacheStrategyExtensions 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
            throw new Exception("未实现或缓存不支持前缀方式获取缓存！");
        }

        public static async Task<IList<T>> GetAllByPrefixAsync<T>(this IBaseObjectCacheStrategy obj, string key)
        {
            if (obj is RedisObjectCacheStrategy)
            {
                var _obj = obj as RedisObjectCacheStrategy;
                return await _obj.GetAllByPrefixAsync<T>(key);
            }
            throw new Exception("未实现或缓存不支持前缀方式获取缓存！");
        }
    }
}
