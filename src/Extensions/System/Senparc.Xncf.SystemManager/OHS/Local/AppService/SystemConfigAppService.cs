using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.Ncf.Core.AppServices;
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
        public async Task<StringAppResponse> UpdateNeuCharAccount(SystemConfig_Request request)
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
    }
}
