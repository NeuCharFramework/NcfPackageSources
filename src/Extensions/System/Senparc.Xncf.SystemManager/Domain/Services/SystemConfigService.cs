using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Log;
using Senparc.Ncf.Service;
using Senparc.NeuChar.App.AppStore;
using Senparc.Xncf.SystemManager.ACL.Repository;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.SystemManager.Domain.Service
{
    public class SystemConfigService : ClientServiceBase<SystemConfig>/*, ISystemConfigService*/
    {
        public SystemConfigService(SystemConfigRepository systemConfigRepo, IServiceProvider serviceProvider)
            : base(systemConfigRepo, serviceProvider)
        {

        }

        public SystemConfig Init(out string errorMsg, string systemName = null, string neuCharAppKey = null, string neuCharAppSecret = null)
        {
            errorMsg = null;

            var systemConfig = GetObject(z => true);
            if (systemConfig != null)
            {
                return null;
            }

            var developerId = 0;
            string appKey = null;
            string appSecret = null;
            systemName ??= "NCF - Template Project";

            if (!neuCharAppKey.IsNullOrEmpty())
            {
                //校验并获取 NeuCharDeveloperId
                var p2pCodeUrl = $"{Senparc.NeuChar.App.AppStore.Config.DefaultDomainName}/api/GetPassport?appKey={neuCharAppKey}&secret={neuCharAppSecret}";
                var messageResult = Senparc.CO2NET.HttpUtility.Post.PostGetJson<PassportResult>(_serviceProvider, p2pCodeUrl, formData: new Dictionary<string, string>(), encoding: Encoding.UTF8);

                if (messageResult.Result == AppResultKind.成功)
                {
                    developerId = messageResult.Data.DeveloperId;
                    appKey = messageResult.Data.AppKey;
                    appSecret = messageResult.Data.Secret;
                }
                else
                {
                    errorMsg = "AppKey 或 AppSecret 不正确！错误信息未被记录，请重新设置！";
                }
            }

            systemConfig = new SystemConfig(systemName, null, null, null, false, developerId, appKey, appSecret);
            SaveObject(systemConfig);

            return systemConfig;
        }

        public override void SaveObject(SystemConfig obj)
        {
            base.SaveObject(obj);
            LogUtility.WebLogger.InfoFormat("SystemConfig 被编辑：{0}", obj.ToJson());

            //清除缓存
            var fullSystemConfigCache = _serviceProvider.GetService<FullSystemConfigCache>();
            //示范同步缓存锁
            using (fullSystemConfigCache.Cache.BeginCacheLock(FullSystemConfigCache.CACHE_KEY, ""))
            {
                fullSystemConfigCache.RemoveCache();
            }
        }

        public override async Task SaveObjectAsync(SystemConfig obj)
        {
            await base.SaveObjectAsync(obj);
            LogUtility.WebLogger.InfoFormat("SystemConfig 被编辑：{0}", obj.ToJson());

            //清除缓存
            var fullSystemConfigCache = _serviceProvider.GetService<FullSystemConfigCache>();
            //示范同步缓存锁
            using (await fullSystemConfigCache.Cache.BeginCacheLockAsync(FullSystemConfigCache.CACHE_KEY, ""))
            {
                fullSystemConfigCache.RemoveCache();
            }
        }
    }
}

