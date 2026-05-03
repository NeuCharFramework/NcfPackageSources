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
    public FirmwareUpdateAppService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    [FunctionRender("镜像设置", "开关自动从 GitHub 同步 NCF 安装包，并设置检查周期（1–24 小时）", typeof(Register))]
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

    [FunctionRender("立即同步", "立即从 GitHub 拉取最新 Release 并更新本地 NcfPackages 与 latest-release.json", typeof(Register))]
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
