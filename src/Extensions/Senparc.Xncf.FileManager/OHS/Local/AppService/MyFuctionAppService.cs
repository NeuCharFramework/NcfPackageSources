/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MyFuctionAppService.cs
    File description: MyFuctionAppService related implementation

    Created by: Senparc - 20250105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.3.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.8.5 Localized file management and upload surfaces

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.FileManager.Domain.Services;
using Senparc.Xncf.FileManager.OHS.Local.PL;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Senparc.Xncf.FileManager.OHS.Local.AppService
{
    public class MyFuctionAppService: AppServiceBase
    {
        private ColorService _colorService;
        /// <summary>
        /// Initializes the sample FileManager function service.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        /// <param name="colorService">The color service dependency.</param>
        public MyFuctionAppService(IServiceProvider serviceProvider, ColorService colorService) : base(serviceProvider)
        {
            _colorService = colorService;
        }

        [FunctionRender(typeof(FileManagerResource), "Function.Sample.Name", "Function.Sample.Description", typeof(Register))]
        /// <summary>
        /// Evaluates the requested arithmetic operation and optional power transformations.
        /// </summary>
        /// <param name="request">The operands, operator, display name, and optional powers.</param>
        /// <returns>A string response containing the calculated result and operation log.</returns>
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

                logger.Append($"进行运算：{request.Number1} {theOperator} {request.Number2} = {calcResult}");

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
