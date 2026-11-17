/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SystemConfigAppService.cs
    File description: SystemConfigAppService related implementation

    Created by: Senparc - 20240827

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260707
    Change description: v0.14.2-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260715
    Change description: v0.14.2-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260717
    Change description: v0.15.0-preview3 integrate shared resource localization and improve feature text for the SystemManager module

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.CO2NET.Cache;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Config;
using Senparc.Xncf.SystemManager.Domain.Service;
using Senparc.Xncf.SystemManager.OHS.Local.PL;

namespace Senparc.Xncf.SystemManager.OHS.Local
{
    public class SystemConfigAppService : AppServiceBase
    {
        SystemConfigService _systemConfigService;
        public SystemConfigAppService(IServiceProvider serviceProvider, SystemConfigService systemConfigService) : base(serviceProvider)
        {
            _systemConfigService = systemConfigService;
        }

        [FunctionRender(typeof(SystemManagerResource), "Function.SystemManager.UpdateCloudAccount.Name", "Function.SystemManager.UpdateCloudAccount.Description", typeof(Register))]
        public async Task<StringAppResponse> UpdateNeuCharAccount(SystemConfig_UpdateNeuCharAccountRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                try
                {

                    return await _systemConfigService.UpdateNeuCharAccount(request.AppKey, request.AppSecret);
                }
                catch (Exception ex)
                {
                    logger.Append(ex.ToString());
                    return "更新 NeuChar 云账户信息失败：" + ex.Message;
                }
            });
        }

        [FunctionRender(typeof(SystemManagerResource), "Function.SystemManager.ViewTempLog.Name", "Function.SystemManager.ViewTempLog.Description", typeof(Register))]
        public async Task<StringAppResponse> GetRequestTempLog(SystemConfig_GetRequestTempLogRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                var requestTempId = request?.RequestTempId?.Trim();
                if (string.IsNullOrWhiteSpace(requestTempId))
                {
                    response.Data = logger.Append("RequestTempId 不能为空。");
                    return response.Data;
                }

                var cache = this.ServiceProvider.GetObjectCacheStrategyInstance();
                var log = await cache.GetAsync<string>(requestTempId);

                if (string.IsNullOrWhiteSpace(log))
                {
                    var cacheMinutes = SiteConfig.SenparcCoreSetting.RequestTempLogCacheMinutes;
                    response.Data = logger.Append($"未找到对应的暂存日志：{requestTempId}。可能日志已过期，或 RequestTempLogCacheMinutes 当前配置为 {cacheMinutes}（<=0 表示不启用缓存）。");
                    return response.Data;
                }

                response.Data = log.Replace("\r\n", "<br />").Replace("\n", "<br />");
                return response.Data;
            });
        }
    }
}
