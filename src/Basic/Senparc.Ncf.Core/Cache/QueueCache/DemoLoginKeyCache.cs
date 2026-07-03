/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DemoLoginKeyCache.cs
    文件功能描述：DemoLoginKeyCache 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Cache
{
    [Serializable]
    public class DemoLoginKeyCacheData
    {
        public string OpenId { get; set; }
        public DateTime AddTime { get; set; }
        public QrCodeLoginDataType QrCodeLoginDataType { get; set; }
        public DemoLoginKeyCacheData(string openId, QrCodeLoginDataType qrCodeLoginDataType)
        {
            OpenId = openId;
            QrCodeLoginDataType = qrCodeLoginDataType;
            AddTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 登录许可缓存（缓存数据：UserId）
    /// </summary>
    public interface IDemoLoginKeyCache : IQueueCache<DemoLoginKeyCacheData>
    {

    }

    [Serializable]
    public class DemoLoginKeyCache : QueueCache<DemoLoginKeyCacheData>, IDemoLoginKeyCache
    {
        private const string cacheKey = "DemoLoginKeyCache";
        private const int timeoutSeconds = 5 * 60;
        private DemoLoginKeyCache()
         : base(cacheKey, timeoutSeconds)
        {
        }

        public static async Task<DemoLoginKeyCache> CreateAsync()
        {
            var instance = new DemoLoginKeyCache();

            // 初始化父类中的异步初始化操作  
            await instance.InitializeAsync();

            return instance;
        }

        private async Task InitializeAsync()
        {
            // 调用父类的静态工厂方法进行异步初始化  
            var queueCache = await QueueCache<DemoLoginKeyCacheData>.CreateAsync(cacheKey, timeoutSeconds);

            // 将初始化的结果赋值给当前实例  
            this.MessageQueue = queueCache.MessageQueue;
            this.MessageCollection = queueCache.MessageCollection;
        }

        public override QueueCacheData<DemoLoginKeyCacheData> Get(string key, bool removeDataWhenExist = true)
        {
            var value = base.Get(key,removeDataWhenExist);
            if (value != null)
            {
                base.Remove(key);//一次性有效
            }
            return value;
        }
    }
}
