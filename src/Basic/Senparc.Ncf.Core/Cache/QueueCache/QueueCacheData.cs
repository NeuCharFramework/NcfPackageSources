/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：QueueCacheData.cs
    文件功能描述：QueueCacheData 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache
{
    [Serializable]
    public class QueueCacheData<T>
    {
        public string Key { get; set; }

        public DateTime LastActiveTime { get; set; }

        public T Data { get; set; }

        public QueueCacheData(string key)
        {
            Key = key;
            LastActiveTime = DateTime.Now;
        }
    }
}