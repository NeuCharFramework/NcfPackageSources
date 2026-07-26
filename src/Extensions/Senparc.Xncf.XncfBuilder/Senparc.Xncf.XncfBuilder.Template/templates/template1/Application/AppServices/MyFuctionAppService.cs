/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MyFuctionAppService.cs
    文件功能描述：XNCF 模板示例实现
    
    
    创建标识：Senparc - 20211031
    
    修改标识：Senparc - 20260726
    修改描述：v1.1.0 补充示例模板 EventBus 请求-响应回环与多语言能力

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

        [FunctionRender("我的函数", "我的函数的注释", typeof(Register))]
        public async Task<StringAppResponse> Calculate(MyFunction_CaculateRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                /* 页面上点击“执行”后，将调用这里的方法
                  *
                  * 参数说明：
                  * response：已经初始化后的返回结果
                  * logger：日志
                  * 
                  * 如果直接对 response 的属性修改，则最终 return null，
                  * 否则可以返回一个新的 response 对象，系统将自动覆盖原有对象
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

        /// <summary>
        /// 验证当前 XNCF 内部 EventBus 的请求、处理、派生响应和关联等待链路。
        /// 此方法不读取数据库、配置、文件、用户信息或其他模块数据。
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
