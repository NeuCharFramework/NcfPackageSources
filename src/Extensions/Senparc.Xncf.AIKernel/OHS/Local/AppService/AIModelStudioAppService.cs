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

        //[FunctionRender("Test AI model", "Test the set model", typeof(Register))]
        //public async Task<StringAppResponse> RunModelAsync(AIModelStudioRequest_RunModelAsync request)
        //{
        //    return await this.GetStringResponseAsync(async (response, logger) =>
        //    {
        //        await request.LoadData(ServiceProvider);//Load data

        //        var msg = new StringBuilder();

        //        var selectedItems = request.Model.Items.Where(z => request.Model.SelectedValues.Contains(z.Value));

        //        if (selectedItems.Count() == 0)
        //        {
        //            throw new SenparcAiException("Please select at least one model!");
        //        }

        //        foreach (var selectedItem in selectedItems)
        //        {
        //            msg.AppendLine($"Testing model: {selectedItem.Value}");
        //            try
        //            {
        //                var aiResult = await _aIModelService.RunModelsync(selectedItem.BindData as SenparcAiSetting, request.Prompt);
        //                msg.AppendLine($"Model test successful, return information: {aiResult.Output}");
        //            }
        //            catch (Exception ex)
        //            {
        //                msg.AppendLine($"Model test failed: {ex.Message}");
        //            }
        //            msg.AppendLine("--------------------------");
        //        }

        //        response.Data = msg.ToString().Replace("\r", "<br />").Replace("\n", "");
        //        logger.Append($"Test completed, return information:\r\n{msg.ToString()}");
        //        return null;
        //    });
        //}
    }
}
