using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using Senparc.Xncf.XncfModuleManager.OHS.Local.PL;

namespace Senparc.Xncf.XncfModuleManager.OHS.Local.AppService
{
    public class XncfStateAppService : AppServiceBase
    {
        private readonly XncfModuleServiceExtension _xncfModuleServiceExtension;
        private readonly Senparc.Ncf.Service.XncfModuleService _xncfModuleService;

        public XncfStateAppService(
            IServiceProvider serviceProvider,
            XncfModuleServiceExtension xncfModuleServiceExtension,
            Senparc.Ncf.Service.XncfModuleService xncfModuleService) : base(serviceProvider)
        {
            _xncfModuleServiceExtension = xncfModuleServiceExtension;
            _xncfModuleService = xncfModuleService;
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

        [FunctionRender("安装并开放 XNCF 模块", "安装指定 XNCF 模块，并将模块状态切换为【开放】", typeof(Register))]
        public async Task<StringAppResponse> InstallAndOpenModule(XncfState_InstallAndOpenModuleRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                var selectedRegisterUid = request.XncfModule.SelectedValues.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(selectedRegisterUid))
                {
                    response.Data = logger.Append("请先选择需要安装的 XNCF 模块。");
                    return response.Data;
                }

                var register = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == selectedRegisterUid);
                if (register == null)
                {
                    response.Data = logger.Append("未找到指定的 XNCF 模块。\r\n");
                    return response.Data;
                }

                response.Data = logger.Append($"目标模块：{register.MenuName} ({register.Name})\r\nUID：{register.Uid}\r\n");

                try
                {
                    var installResult = await _xncfModuleServiceExtension.InstallModuleAsync(register.Uid, true).ConfigureAwait(false);
                    response.Data += logger.Append("安装完成。\r\n");
                    response.Data += logger.Append($"安装结果：{installResult.scanAndInstallResult}\r\n");
                }
                catch (Exception ex)
                {
                    response.Data += logger.Append($"安装阶段提示：{ex.Message}\r\n");
                }

                var xncfModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == register.Uid).ConfigureAwait(false);
                if (xncfModule == null)
                {
                    response.Data += logger.Append("模块尚未写入系统安装表，请先检查安装日志。\r\n");
                    return response.Data.Replace("\r\n", "<br />");
                }

                if (xncfModule.State != XncfModules_State.开放)
                {
                    xncfModule.UpdateState(XncfModules_State.开放);
                    await _xncfModuleService.SaveObjectAsync(xncfModule).ConfigureAwait(false);
                    response.Data += logger.Append("模块状态已切换为【开放】。\r\n");
                }
                else
                {
                    response.Data += logger.Append("模块当前已是【开放】状态，无需重复切换。\r\n");
                }

                return response.Data.Replace("\r\n", "<br />");
            });
        }

        [FunctionRender("获取全部 XNCF 模块信息", "获取系统中全部 XNCF 模块名称和描述（含状态）", typeof(Register))]
        public async Task<StringAppResponse> GetAllXncfModules()
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                var installedModules = await _xncfModuleServiceExtension.GetFullListAsync(z => true).ConfigureAwait(false);
                var installedMap = installedModules.ToDictionary(z => z.Uid, z => z);

                var registers = XncfRegisterManager.RegisterList
                    .Where(z => !z.IgnoreInstall)
                    .OrderBy(z => z.MenuName)
                    .ToList();

                response.Data = logger.Append($"系统共发现 {registers.Count} 个可安装 XNCF 模块：\r\n");
                foreach (var register in registers)
                {
                    installedMap.TryGetValue(register.Uid, out var installedModule);
                    var stateText = installedModule?.State.ToString() ?? "未安装";
                    response.Data += logger.Append($"- 名称：{register.MenuName} / {register.Name}\r\n");
                    response.Data += logger.Append($"  UID：{register.Uid}\r\n");
                    response.Data += logger.Append($"  描述：{register.Description}\r\n");
                    response.Data += logger.Append($"  状态：{stateText}\r\n\r\n");
                }

                return response.Data.Replace("\r\n", "<br />");
            });
        }
    }
}
