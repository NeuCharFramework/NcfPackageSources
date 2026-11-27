/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MyFuctionAppService.cs
    File description: XNCF template sample implementation

    Created by: Senparc - 20211031

    Modified by: Senparc - 20260726
    Change description: v1.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v1.1.15 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261124
    Change description: v1.1.15 Integrated localization, tooling, and runtime improvements across the module.

    Modified by: Senparc - 20261127
    Change description: v1.1.16 Improve source documentation, headers, and module metadata consistency.

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

        /// <summary>
        /// Initializes the sample function application service.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        /// <param name="eventBusRequestClient">The request client used by the round-trip sample.</param>
        public MyFuctionAppService(
            IServiceProvider serviceProvider,
            IEventBusRequestClient eventBusRequestClient) : base(serviceProvider)
        {
            _eventBusRequestClient = eventBusRequestClient;
        }

        /// <summary>
        /// Calculates a value using the requested arithmetic operator and optional square/cube
        /// transformations, returning the operation trace through the application-service response.
        /// Division by zero and unsupported operators are reported as unsuccessful responses.
        /// </summary>
        /// <param name="request">The operands, operator, display name, and optional powers to apply.</param>
        /// <returns>A response containing the formatted calculation result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
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
                                "Frontend.Template_XncfName.DivisorCannotBeZero",
                                "The divisor cannot be zero.");
                            return null;
                        }
                        calcResult = calcResult / request.Number2;
                        break;
                    default:
                        response.Success = false;
                        response.ErrorMessage = Template_XncfNameResource.Format(
                            "Frontend.Template_XncfName.UnknownOperatorValue",
                            "Unknown operator: {0}",
                            theOperator);
                        return null;
                }

                logger.Append($"Calculation: {request.Number1} {theOperator} {request.Number2} = {calcResult}");

                Action<int> raisePower = power =>
                {
                    if ((request.Power ?? Array.Empty<string>()).Contains(power.ToString()))
                    {
                        var oldValue = calcResult;
                        calcResult = Math.Pow(calcResult, power);
                        logger.Append($"Power {power}: {oldValue}{(power == 2 ? "²" : "³")} = {calcResult}");
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
        /// Sends a sample request through the event-bus request client and returns the response
        /// metadata, elapsed time, and event depth. A timeout is converted into an unsuccessful
        /// application-service response.
        /// </summary>
        /// <param name="request">The request payload; the sample currently uses the event bus rather than its fields.</param>
        /// <returns>A response containing the event-bus round-trip result or timeout information.</returns>
        /// <exception cref="TimeoutException">Handled internally when no event-bus response arrives within five seconds.</exception>
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
