/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MyFuctionAppService.cs
    File description: MyFuctionAppService related implementation

    Created by: Senparc - 20250113

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.3.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261124
    Change description: v0.4.13 Integrated localization, tooling, and runtime improvements across the module.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.SenMapic.Domain.Services;
using Senparc.Xncf.SenMapic.Domain.SiteMap;
using Senparc.Xncf.SenMapic.OHS.Local.PL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using ModelContextProtocol.Server;


namespace Senparc.Xncf.SenMapic.OHS.Local.AppService
{
    //[McpServerToolType]
    public class MyFuctionAppService: AppServiceBase
    {
        private ColorService _colorService;
        private SenMapicTaskService _senMapicTaskService;
        public MyFuctionAppService(IServiceProvider serviceProvider, ColorService colorService, SenMapicTaskService senMapicTaskService) : base(serviceProvider)
        {
            _colorService = colorService;
            _senMapicTaskService = senMapicTaskService;
        }


        //[McpServerTool]
        [FunctionRender(typeof(SenMapicResource), "Function.SenMapic.Crawl.Name", "Function.SenMapic.Crawl.Description", typeof(Register))]
        public async Task<StringAppResponse> WebSpider(MyFunction_SenMapicRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
        //Legacy comment translated to English.
        //             request.Url, 5, 10, request.Deepth, request.PageNumber, true);
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //Legacy comment translated to English.

        //      await _senMapicTaskService.StartTaskAsync(crawTask);

            List<KeyValuePair<ContentType, string>> contentMap = new List<KeyValuePair<ContentType, string>>();

            var maxPageCount = request.GetEffectivePageCount();
            Console.WriteLine($"Crawl: {request.Url}, depth: {request.Deepth}, max pages: {maxPageCount}");

            var senMapicEngine = new SenMapicEngine(
                                serviceProvider: base.ServiceProvider,
                                urls: new[] { request.Url },
                                maxThread: 4,
                                maxBuildMinutesForSingleSite: 5,
                                maxDeep: request.Deepth,
                                maxPageCount: maxPageCount);

            var senMapicResult = senMapicEngine.Build();

            var htmlResult =  string.Join("\r\n\r\n", senMapicResult.Values.Select(z=> " - "+ z.Url +":\n"+ z.MarkDownHtmlContent).ToArray());
            logger.Append(htmlResult);
            System.Console.WriteLine("htmlResult: "+htmlResult);
            return htmlResult;
            });
        }

        [FunctionRender(typeof(SenMapicResource), "Function.Sample.Name", "Function.Sample.Description", typeof(Register))]
        public async Task<StringAppResponse> Calculate(MyFunction_CaculateRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                /* Legacy comment translated to English.

                  *

                  * parametersdescription：

                  * Legacy comment translated to English.

                  * Legacy comment translated to English.

                  *

                  * Legacy comment translated to English.

                  * Legacy comment translated to English.

                  */

                double calcResult = request.Number1;
                var theOperator = request.Operator;
                switch (theOperator)
                {
                    case "+":
                        calcResult = calcResult + request.Number2;
                        break;
                    case "-":
                        calcResult = calcResult - request.Number2;
                        break;
                    case "×":
                        calcResult = calcResult * request.Number2;
                        break;
                    case "÷":
                        if (request.Number2 == 0)
                        {
                            response.Success = false;
                            response.ErrorMessage = "The divisor cannot be zero!";
                            return null;
                        }
                        calcResult = calcResult / request.Number2;
                        break;
                    default:
                        response.Success = false;
                        response.ErrorMessage = $"Unknown operator: {theOperator}";
                        return null;
                }

                logger.Append($"Calculation: {request.Number1} {theOperator} {request.Number2} = {calcResult}");

                Action<int> raisePower = power =>
                {
                    if ((request.Power ?? Array.Empty<string>()).Contains(power.ToString()))
                    {
                        var oldValue = calcResult;
                        calcResult = Math.Pow(calcResult, power);
                        logger.Append($"Calculated power {power}: {oldValue}{(power == 2 ? "²" : "³")} = {calcResult}");
                    }
                };

                raisePower(2);
                raisePower(3);

                response.Data = $"[{request.Name}] Result: {calcResult}. See the log for calculation details.";
                return null;
            });
        }
    }
}
