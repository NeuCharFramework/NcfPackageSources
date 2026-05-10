using Microsoft.EntityFrameworkCore;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Shared.Abstractions.Events;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.XncfBuilder.Abstractions;
using Senparc.Xncf.XncfBuilder.Abstractions.Events;
using Template_OrgName.Xncf.Template_XncfName.Domain.Services;
using Template_OrgName.Xncf.Template_XncfName.OHS.Local.PL;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Template_OrgName.Xncf.Template_XncfName.OHS.Local.AppService
{
    public class MyFuctionAppService: AppServiceBase
    {
        private ColorService _colorService;
        public MyFuctionAppService(IServiceProvider serviceProvider, ColorService colorService) : base(serviceProvider)
        {
            _colorService = colorService;
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

        [FunctionRender("通过 EventBus 查询 XNCF 模块清单", "发布 XncfModulesInventoryRequestEvent，由 Senparc.Xncf.XncfBuilder 汇总已安装与未安装（或未对齐版本）模块。", typeof(Register))]
        public async Task<StringAppResponse> RequestXncfModulesInventoryViaEventBus(MyFunction_XncfModulesInventoryRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                var waiter = ServiceProvider.GetRequiredService<IXncfModulesInventoryRequestWaiter>();
                var eventBus = ServiceProvider.GetRequiredService<IEventBus>();
                var requestId = Guid.NewGuid().ToString("N");
                waiter.RegisterRequest(requestId);
                logger.Append($"已注册等待 RequestId={requestId}，准备发布 {nameof(XncfModulesInventoryRequestEvent)}。");

                await eventBus.PublishAsync(new XncfModulesInventoryRequestEvent(requestId)).ConfigureAwait(false);

                (bool ok, string message, XncfModuleInventoryItem[] installed, XncfModuleInventoryItem[] notInstalled) =
                    await waiter.WaitForResponseAsync(requestId, TimeSpan.FromSeconds(15)).ConfigureAwait(false);

                if (!ok)
                {
                    response.Success = false;
                    response.ErrorMessage = message;
                    logger.Append($"查询失败：{message}");
                    return null;
                }

                var sb = new StringBuilder();
                sb.AppendLine(message);
                sb.AppendLine();
                sb.AppendLine($"已安装当前版本（{installed.Length}）：");
                foreach (var x in installed)
                {
                    sb.AppendLine($"- {x.MenuName} / {x.ModuleName}  UID={x.Uid}  版本={x.RegisterVersion}  状态={x.StateDescription}");
                }

                sb.AppendLine();
                sb.AppendLine($"未安装或未对齐版本（{notInstalled.Length}）：");
                foreach (var x in notInstalled)
                {
                    sb.AppendLine($"- {x.MenuName} / {x.ModuleName}  UID={x.Uid}  程序集版本={x.RegisterVersion}  库版本={x.DatabaseVersion ?? "-"}  说明={x.StateDescription}");
                }

                logger.Append(sb.ToString());
                response.Data = sb.ToString().Replace("\r\n", "<br />");
                return null;
            });
        }
    }
}
