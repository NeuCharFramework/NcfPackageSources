using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.AI.Kernel;
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

        [FunctionRender("测试 AI 模型", "测试已经设置的模型", typeof(Register))]
        public async Task<StringAppResponse> RunModelAsync(AIModelStudioRequest_RunModelAsync request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var msg = new StringBuilder();

                var selectedItems = request.Model.Items.Where(z => request.Model.SelectedValues.Contains(z.Value));

                foreach (var selectedItem in selectedItems)
                {
                    msg.AppendLine($"正在测试模型：{selectedItem.Text}（{selectedItem.Value}）");
                    try
                    {
                        var aiResult = await _aIModelService.RunModelsync(selectedItem.BindData as SenparcAiSetting, request.Prompt);
                        msg.AppendLine($"模型测试成功：{aiResult}");
                    }
                    catch (Exception ex)
                    {
                        msg.AppendLine($"模型测试失败：{ex.Message}");
                    }
                    msg.AppendLine("--------------------------");
                }

                response.Data = msg.ToString();
                return response.Data;
            });
        }
    }
}
