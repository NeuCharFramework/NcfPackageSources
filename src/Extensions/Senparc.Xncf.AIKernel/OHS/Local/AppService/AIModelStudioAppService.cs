using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.AI.Exceptions;
using Senparc.AI.Kernel;
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
