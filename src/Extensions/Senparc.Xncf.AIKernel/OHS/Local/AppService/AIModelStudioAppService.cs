/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AIModelStudioAppService.cs
    文件功能描述：AIModelStudioAppService 服务逻辑
    
    
    创建标识：Senparc - 20240131
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.AI.Exceptions;
using Senparc.AI.AgentKernel;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.AIKernel.OHS.Local.PL;

namespace Senparc.Xncf.AIKernel.OHS.Local.AppService
{
    public class AIModelStudioAppService : AppServiceBase
    {
        private readonly AIModelService _aIModelService;

        public AIModelStudioAppService(IServiceProvider serviceProvider, AIModelService aIModelService) : base(serviceProvider)
        {
            this._aIModelService = aIModelService;
        }

        //[FunctionRender("测试 AI 模型", "测试已经设置的模型", typeof(Register))]
        //public async Task<StringAppResponse> RunModelAsync(AIModelStudioRequest_RunModelAsync request)
        //{
        //    return await this.GetStringResponseAsync(async (response, logger) =>
        //    {
        //        await request.LoadData(ServiceProvider);//加载数据

        //        var msg = new StringBuilder();

        //        var selectedItems = request.Model.Items.Where(z => request.Model.SelectedValues.Contains(z.Value));

        //        if (selectedItems.Count() == 0)
        //        {
        //            throw new SenparcAiException("请至少选择一个模型！");
        //        }

        //        foreach (var selectedItem in selectedItems)
        //        {
        //            msg.AppendLine($"正在测试模型：{selectedItem.Value}");
        //            try
        //            {
        //                var aiResult = await _aIModelService.RunModelsync(selectedItem.BindData as SenparcAiSetting, request.Prompt);
        //                msg.AppendLine($"模型测试成功，返回信息：{aiResult.Output}");
        //            }
        //            catch (Exception ex)
        //            {
        //                msg.AppendLine($"模型测试失败：{ex.Message}");
        //            }
        //            msg.AppendLine("--------------------------");
        //        }

        //        response.Data = msg.ToString().Replace("\r", "<br />").Replace("\n", "");
        //        logger.Append($"测试完成，返回信息：\r\n{msg.ToString()}");
        //        return null;
        //    });
        //}
    }
}
