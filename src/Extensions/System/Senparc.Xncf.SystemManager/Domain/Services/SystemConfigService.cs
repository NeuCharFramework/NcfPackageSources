using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
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

        /// <summary>
        /// 更新 NeuChar 账号
        /// </summary>
        /// <param name="neuCharAppKey"></param>
        /// <param name="neuCharAppSecret"></param>
        /// <returns></returns>
        public async Task<string> UpdateNeuCharAccount(string neuCharAppKey, string neuCharAppSecret)
        {
            var developerId = 0;
            string appKey = null;
            string appSecret = null;

            var systemConfig = await base.GetObjectAsync(z => true);

            if (!neuCharAppKey.IsNullOrEmpty() && !neuCharAppSecret.IsNullOrEmpty())
            {
                //校验并获取 NeuCharDeveloperId
                var passportUrl = $"{Senparc.NeuChar.App.AppStore.Config.DefaultDomainName}/api/GetPassport";
                //Console.WriteLine("passport:" + (passportUrl));

                var data = new Dictionary<string, string>() {
                    { "appKey",neuCharAppKey },
                    { "secret" ,neuCharAppSecret}
                  };

                var messageResult = await Senparc.CO2NET.HttpUtility.Post.PostFileGetJsonAsync<PassportResult>(_serviceProvider, passportUrl, postDataDictionary: data, encoding: Encoding.UTF8);

                if (messageResult.Result == AppResultKind.成功)
                {
                    developerId = messageResult.Data.DeveloperId;
                    appKey = messageResult.Data.AppKey;
                    appSecret = messageResult.Data.Secret;
                    systemConfig.UpdateNeuCharAccount(developerId, appKey, appSecret);
                    await base.SaveObjectAsync(systemConfig);

                    SenparcTrace.SendCustomLog("完成开发者信息认证", $"DeveloperId:{developerId}");

                    return "核验成功！信息已保存！";
                }
                else
                {
                    return "AppKey 或 AppSecret 不正确！错误信息未被记录，请重新设置！";
                }
            }
            else
            {
                return "请提供有效的 AppKey 或 AppSecret！";
            }
        }

        public SystemConfig Init(string systemName = null)
        {
            var systemConfig = GetObject(z => true);
            if (systemConfig != null)
            {
                return null;
            }

            var developerId = 0;
            string appKey = null;
            string appSecret = null;
            systemName ??= "NCF - Template Project";

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

