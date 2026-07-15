/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SystemConfigAppService.cs
    文件功能描述：SystemConfigAppService 相关实现
    
    
    创建标识：Senparc - 20240827
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260707
    修改描述：v0.14.2-preview2 新增 RequestTempId 暂存日志查询能力并补齐请求模型

    修改标识：Senparc - 20260715
    修改描述：v0.14.2-preview2 升级 Senparc.AI 至 0.27.3 与 Senparc.AI.AgentKernel 至 0.1.10

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

        [FunctionRender("更新 NeuChar 云账户信息", "使用 https://www.neuchar.com/Developer/Developer 页面中提供的 AppKey、Secret 信息，绑定 NeuChar 云账号，激活更多高级功能", typeof(Register))]
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

        [FunctionRender("查看 RequestTempId 暂存日志", "根据 AppService 返回的 RequestTempId 查询暂存日志（受 RequestTempLogCacheMinutes 时效配置影响）", typeof(Register))]
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
