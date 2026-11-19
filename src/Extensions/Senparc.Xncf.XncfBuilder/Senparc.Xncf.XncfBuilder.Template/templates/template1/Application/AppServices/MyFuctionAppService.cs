/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MyFuctionAppService.cs
    File description: XNCF template sample implementation

    Created by: Senparc - 20211031

    Modified by: Senparc - 20260726
    Change description: v1.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v1.1.15 Localized XNCF Builder tools and template surfaces

----------------------------------------------------------------*/
using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Shared.Abstractions.Events;
using Template_OrgName.Xncf.Template_XncfName.Application.DTOs.Request;
using Template_OrgName.Xncf.Template_XncfName.Application.Events;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace Template_OrgName.Xncf.Template_XncfName.Application.AppServices
{
    public class MyFuctionAppService: AppServiceBase
    {
        private readonly IEventBusRequestClient _eventBusRequestClient;

        public MyFuctionAppService(
            IServiceProvider serviceProvider,
            IEventBusRequestClient eventBusRequestClient) : base(serviceProvider)
        {
            _eventBusRequestClient = eventBusRequestClient;
        }

        [FunctionRender(
            typeof(Template_XncfNameResource),
            "Function.Sample.Name",
            "Function.Sample.Description",
            typeof(Register))]
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
                            response.ErrorMessage = Template_XncfNameResource.Get(
                                "Frontend.061",
                                "The divisor cannot be zero.");
                            return null;
                        }
                        calcResult = calcResult / request.Number2;
                        break;
                    default:
                        response.Success = false;
                        response.ErrorMessage = Template_XncfNameResource.Format(
                            "Frontend.062",
                            "Unknown operator: {0}",
                            theOperator);
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

                response.Data = Template_XncfNameResource.Format(
                    "Function.Sample.Result",
                    "[{0}] result: {1}. See the log for the calculation steps.",
                    request.Name,
                    calcResult);
                return null;
            });
        }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        [FunctionRender(
            typeof(Template_XncfNameResource),
            "Function.EventBusRoundTrip.Name",
            "Function.EventBusRoundTrip.Description",
            typeof(Register))]
        public async Task<StringAppResponse> EventBusRoundTrip(EventBusRoundTripRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                var requestEvent = new InternalEventBusRoundTripRequest(DateTime.UtcNow);
                var startedAt = DateTime.UtcNow;

                try
                {
                    var responseEvent = await _eventBusRequestClient.RequestAsync<InternalEventBusRoundTripResponse>(
                        requestEvent,
                        TimeSpan.FromSeconds(5),
                        CancellationToken);

                    var elapsedMilliseconds = (DateTime.UtcNow - startedAt).TotalMilliseconds;
                    var result = Template_XncfNameResource.Format(
                        "Function.EventBusRoundTrip.Success",
                        "EventBus round-trip succeeded. RequestId: {0}; ParentEventId: {1}; Depth: {2}; Elapsed: {3:F2} ms.",
                        responseEvent.RequestId.ToString("N"),
                        responseEvent.ParentEventId?.ToString("N") ?? "-",
                        responseEvent.Depth,
                        elapsedMilliseconds);

                    logger.Append(result);
                    response.Data = result;
                }
                catch (TimeoutException)
                {
                    response.Success = false;
                    response.ErrorMessage = Template_XncfNameResource.Get(
                        "Function.EventBusRoundTrip.Timeout",
                        "EventBus round-trip timed out. Confirm that the host registered EventBus and scanned this XNCF assembly.");
                    logger.Append(response.ErrorMessage);
                }

                return null;
            });
        }
    }
}
