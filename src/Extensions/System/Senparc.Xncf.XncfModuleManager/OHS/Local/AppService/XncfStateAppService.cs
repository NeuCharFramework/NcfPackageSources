using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.XncfModuleManager.OHS.Local.PL;

namespace Senparc.Xncf.XncfModuleManager.OHS.Local.AppService
{
    public class XncfStateAppService : AppServiceBase
    {
        public XncfStateAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        //[ApiBind]
        [FunctionRender("查看 XNCF 模型的 Function 状态", "查看当前 XNCF 的 Function 载入情况", typeof(Register))]
        public async Task<StringAppResponse> ShowFunctions(XncfState_ShowFunctionsRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                var selectedRegisterUid = request.XncfModule.SelectedValues.FirstOrDefault();
                var register = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == selectedRegisterUid);
                if (register == null)
                {
                    response.Data = logger.Append("未找到指定的 XNCF 模块");
                    return response.Data;
                }

                response.Data = logger.Append($"XNCF 模块：{register.Name}\r\n");

                if (Senparc.Ncf.XncfBase.Register.FunctionRenderCollection.TryGetValue(register.GetType(), out var functionGroup))
                {
                    if (functionGroup == null || functionGroup.Count == 0)
                    {
                        response.Data += logger.Append("当前模块没有注册任何 Function。\r\n");
                        return response.Data;
                    }

                    foreach (var function in functionGroup)
                    {
                        response.Data += logger.Append($"[Function: {function.Value.Key}]\r\n");
                        response.Data += logger.Append($"[内部方法: {function.Key}]\r\n");

                        //response.Data = logger.Append($"名称：{function.Value.FunctionRenderAttribute.Name}\r\n");

                        try
                        {
                            var functionParameterInfos = await FunctionHelper.GetFunctionParameterInfoAsync(base.ServiceProvider, function.Value, true);

                            foreach (var functionBag in functionParameterInfos)
                            {
                                response.Data += logger.Append($"参数：{functionBag.Name}（{functionBag.Description}）\r\n");
                                response.Data += logger.Append($"类型：{functionBag.ParameterType}\r\n\r\n");
                            }
                        }
                        catch (Exception ex)
                        {
                            response.Data += logger.Append($"载入出错：{ex.Message}\r\nStackTrace:{ex.StackTrace}\r\nInnerException:{ex.InnerException}");
                        }

                    }
                }
                else
                {
                    response.Data += logger.Append("当前模块没有注册任何 Function。\r\n");
                }
                return response.Data.Replace("\r\n", "<br />");
            });
        }
    }
}
