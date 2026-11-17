/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: AIModelStudioAppService.cs

    File description: Related implementation.

    

    

    Created by: Senparc - 20240131

    

    Modified by: Senparc - 20260702

    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling



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

        //Legacy comment translated to English.
        //public async Task<StringAppResponse> RunModelAsync(AIModelStudioRequest_RunModelAsync request)
        //{
        //    return await this.GetStringResponseAsync(async (response, logger) =>
        //    {
        //Legacy comment translated to English.

        //        var msg = new StringBuilder();

        //        var selectedItems = request.Model.Items.Where(z => request.Model.SelectedValues.Contains(z.Value));

        //        if (selectedItems.Count() == 0)
        //        {
        //Legacy comment translated to English.
        //        }

        //        foreach (var selectedItem in selectedItems)
        //        {
        //Legacy comment translated to English.
        //            try
        //            {
        //                var aiResult = await _aIModelService.RunModelsync(selectedItem.BindData as SenparcAiSetting, request.Prompt);
        //                msg.AppendLine($"modeltestsuccess，returninformation：{aiResult.Output}");
        //            }
        //            catch (Exception ex)
        //            {
        //                msg.AppendLine($"modeltestfailure：{ex.Message}");
        //            }
        //            msg.AppendLine("--------------------------");
        //        }

        //        response.Data = msg.ToString().Replace("\r", "<br />").Replace("\n", "");
        //        logger.Append($"testcomplete，returninformation：\r\n{msg.ToString()}");
        //        return null;
        //    });
        //}
    }
}
