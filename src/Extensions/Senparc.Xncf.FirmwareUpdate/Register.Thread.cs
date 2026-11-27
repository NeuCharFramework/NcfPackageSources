/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Thread.cs
    File description: Register.Thread related implementation

    Created by: Senparc - 20260504

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260813
    Change description: v0.4.2-preview7 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.11 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.4.11.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Threads;
using Senparc.Xncf.FirmwareUpdate.Domain.Services;

namespace Senparc.Xncf.FirmwareUpdate;

public partial class Register : IXncfThread
{
    /// <summary>
    /// Registers the periodic package-mirror background task and its cancellation/error behavior.
    /// </summary>
    /// <param name="xncfThreadBuilder">The NCF thread builder.</param>
    public void ThreadConfig(XncfThreadBuilder xncfThreadBuilder)
    {
        xncfThreadBuilder.AddThreadInfo(new Ncf.XncfBase.Threads.ThreadInfo(
            name: "NCF 安装包镜像（GitHub → 本地 NcfPackages）",
            intervalTime: TimeSpan.FromMinutes(1),
            task: async (app, threadInfo) =>
            {
                try
                {
                    threadInfo.StoppingToken.ThrowIfCancellationRequested();
                    threadInfo.RecordStory("开始检查镜像任务");

                    using var scope = app.ApplicationServices.CreateScope();
                    var sp = scope.ServiceProvider;

                    var xncfRegisterManager = new XncfRegisterManager(sp);
                    var available = await xncfRegisterManager.CheckXncfAvailable(this).ConfigureAwait(false);
                    if (!available)
                    {
                        threadInfo.RecordStory("模块未启用或不可用，跳过");
                        return;
                    }

                    var mirror = sp.GetRequiredService<NcfPackageMirrorService>();
                    var msg = await mirror.RunAsync(
                        sp,
                        manualTrigger: false,
                        threadInfo.StoppingToken).ConfigureAwait(false);
                    threadInfo.RecordStory(msg);
                    SenparcTrace.SendCustomLog("FirmwareUpdate", msg);
                }
                catch (OperationCanceledException) when (threadInfo.StoppingToken.IsCancellationRequested)
                {
                    threadInfo.RecordStory("应用正在停止，已取消镜像任务。");
                }
                catch (NcfModuleException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    threadInfo.RecordStory("镜像任务异常：" + ex.Message);
                    SenparcTrace.SendCustomLog("FirmwareUpdate", $"{ex.Message}\n{ex.StackTrace}");
                    throw;
                }
                finally
                {
                    threadInfo.RecordStory("镜像检查结束");
                }
            },
            exceptionHandler: ex =>
            {
                SenparcTrace.SendCustomLog("FirmwareUpdate.Thread", $@"{ex.Message}
{ex.StackTrace}
{ex.InnerException?.StackTrace}");
                return Task.CompletedTask;
            }));
    }
}
