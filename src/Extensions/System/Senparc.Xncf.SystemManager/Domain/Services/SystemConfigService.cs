using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Log;
using Senparc.Ncf.Service;
using Senparc.Xncf.SystemManager.ACL.Repository;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.SystemManager.Domain.Service
{
    public class SystemConfigService : ClientServiceBase<SystemConfig>/*, ISystemConfigService*/
    {
        public SystemConfigService(SystemConfigRepository systemConfigRepo, IServiceProvider serviceProvider)
            : base(systemConfigRepo, serviceProvider)
        {

        }

        public SystemConfig Init(string systemName = null)
        {
            var systemConfig = GetObject(z => true);
            if (systemConfig != null)
            {
                return null;
            }

            systemConfig = new SystemConfig(systemName ?? "NCF - Template Project", null, null, null, false);
            SaveObject(systemConfig);

            return systemConfig;
        }

        public override void SaveObject(SystemConfig obj)
        {
            base.SaveObject(obj);
            LogUtility.WebLogger.InfoFormat("SystemConfig ±»±à¼­£º{0}", obj.ToJson());

            //Çå³ý»º´æ
            var fullSystemConfigCache = _serviceProvider.GetService<FullSystemConfigCache>();
            //Ê¾·¶Í¬²½»º´æËø
            using (fullSystemConfigCache.Cache.BeginCacheLock(FullSystemConfigCache.CACHE_KEY, ""))
            {
                fullSystemConfigCache.RemoveCache();
            }
        }

        public override async Task SaveObjectAsync(SystemConfig obj)
        {
            await base.SaveObjectAsync(obj);
            LogUtility.WebLogger.InfoFormat("SystemConfig ±»±à¼­£º{0}", obj.ToJson());

            //Çå³ý»º´æ
            var fullSystemConfigCache = _serviceProvider.GetService<FullSystemConfigCache>();
            //Ê¾·¶Í¬²½»º´æËø
            using (await fullSystemConfigCache.Cache.BeginCacheLockAsync(FullSystemConfigCache.CACHE_KEY, ""))
            {
                fullSystemConfigCache.RemoveCache();
            }
        }
    }
}

