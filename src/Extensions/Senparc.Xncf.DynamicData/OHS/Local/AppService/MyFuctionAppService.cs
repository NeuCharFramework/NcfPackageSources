/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MyFuctionAppService.cs
    File description: MyFuctionAppService related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.6.0-preview2 integrate shared resource localization and improve feature text for the DynamicData module

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.DynamicData.Domain.Services;
using Senparc.Xncf.DynamicData.OHS.Local.PL;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Senparc.Xncf.DynamicData.OHS.Local.AppService
{
    public class MyFuctionAppService: AppServiceBase
    {
        private ColorService _colorService;
        /// <summary>
        /// Initializes the DynamicData calculation sample service.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        /// <param name="colorService">The color service retained for the sample module integration.</param>
        public MyFuctionAppService(IServiceProvider serviceProvider, ColorService colorService) : base(serviceProvider)
        {
            _colorService = colorService;
        }

        /// <summary>
        /// Applies the requested arithmetic operation and optional square/cube transformations.
        /// Division by zero and unsupported operators are returned as unsuccessful function responses.
        /// </summary>
        /// <param name="request">The operands, operator, display name, and optional powers.</param>
        /// <returns>A response containing the formatted calculation result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
        [FunctionRender(typeof(DynamicDataResource), "Function.Sample.Name", "Function.Sample.Description", typeof(Register))]
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
                            response.ErrorMessage = "被除数不能为0！";
                            return null;
                        }
                        calcResult = calcResult / request.Number2;
                        break;
                    default:
                        response.Success = false;
                        response.ErrorMessage = $"未知的运算符：{theOperator}";
                        return null;
                }

                logger.Append($"进行运算：{request.Number1} {theOperator} {request.Number2} = {calcResult}");

                Action<int> raisePower = power =>
                {
                    if ((request.Power ?? Array.Empty<string>()).Contains(power.ToString()))
                    {
                        var oldValue = calcResult;
                        calcResult = Math.Pow(calcResult, power);
                        logger.Append($"进行{power}次方运算：{oldValue}{(power == 2 ? "²" : "³")} = {calcResult}");
                    }
                };

                raisePower(2);
                raisePower(3);

                response.Data = $"【{request.Name}】计算结果：{calcResult}。计算过程请看日志";
                return null;
            });
        }
    }
}
