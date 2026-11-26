/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FirmwareUpdateAppService.cs
    File description: FirmwareUpdateAppService related implementation

    Created by: Senparc - 20260504

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.3.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.11 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Xncf.FirmwareUpdate.Domain.Services;
using Senparc.Xncf.FirmwareUpdate.OHS.Local.PL;

namespace Senparc.Xncf.FirmwareUpdate.OHS.Local.AppService;

public class FirmwareUpdateAppService : AppServiceBase
{
    /// <summary>
    /// Initializes the firmware-update configuration and synchronization application service.
    /// </summary>
    /// <param name="serviceProvider">The dependency provider used by the base service.</param>
    public FirmwareUpdateAppService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    /// <summary>
    /// Saves the automatic mirror enablement and clamps its package synchronization interval
    /// to the supported one-to-24-hour range.
    /// </summary>
    /// <param name="request">The mirror enablement and requested interval.</param>
    /// <returns>A response containing the saved configuration message.</returns>
    [FunctionRender(typeof(FirmwareUpdateResource), "Function.FirmwareUpdate.Settings.Name", "Function.FirmwareUpdate.Settings.Description", typeof(Register))]
    public async Task<StringAppResponse> ConfigureMirror(FirmwareUpdate_ConfigureRequest request)
    {
        return await this.GetStringResponseAsync(async (_, logger) =>
        {
            var configService = ServiceProvider.GetRequiredService<ServiceBase<FirmwareUpdateConfig>>();
            var config = await configService.GetObjectAsync(_ => true).ConfigureAwait(false);
            if (config == null)
            {
                return "未找到配置记录，请确认模块已正确安装。";
            }

            var hours = Math.Clamp(request.UpdateIntervalHours, 1, 24);
            config.AutoMirrorEnabled = request.AutoMirrorEnabled;
            config.UpdateIntervalHours = hours;
            await configService.SaveObjectAsync(config).ConfigureAwait(false);

            var msg = $"已保存：自动镜像={(request.AutoMirrorEnabled ? "开启" : "关闭")}，周期={hours} 小时。本地包目录：{NcfPackageMirrorService.GetLocalPackageRoot()}";
            logger.Append(msg);
            return msg;
        }, saveLogAfterFinished: true, saveLogName: "FirmwareUpdate 镜像设置");
    }

    /// <summary>
    /// Runs the package mirror immediately through the configured mirror service.
    /// </summary>
    /// <param name="_">The invocation request; its current fields are not used.</param>
    /// <returns>A response containing the mirror operation message.</returns>
    [FunctionRender(typeof(FirmwareUpdateResource), "Function.FirmwareUpdate.Sync.Name", "Function.FirmwareUpdate.Sync.Description", typeof(Register))]
    public async Task<StringAppResponse> SyncNow(FirmwareUpdate_SyncNowRequest _)
    {
        return await this.GetStringResponseAsync(async (_, logger) =>
        {
            var mirror = ServiceProvider.GetRequiredService<NcfPackageMirrorService>();
            var msg = await mirror.RunAsync(ServiceProvider, manualTrigger: true).ConfigureAwait(false);
            logger.Append(msg);
            return msg;
        }, saveLogAfterFinished: true, saveLogName: "FirmwareUpdate 立即同步");
    }
}
