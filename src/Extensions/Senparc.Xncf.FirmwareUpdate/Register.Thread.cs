/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.Thread.cs
    文件功能描述：Register.Thread 相关实现
    
    
    创建标识：Senparc - 20260504
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
    public void ThreadConfig(XncfThreadBuilder xncfThreadBuilder)
    {
        xncfThreadBuilder.AddThreadInfo(new Ncf.XncfBase.Threads.ThreadInfo(
            name: "NCF 安装包镜像（GitHub → 本地 NcfPackages）",
            intervalTime: TimeSpan.FromMinutes(1),
            task: async (app, threadInfo) =>
            {
                try
                {
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
                    var msg = await mirror.RunAsync(sp, manualTrigger: false).ConfigureAwait(false);
                    threadInfo.RecordStory(msg);
                    SenparcTrace.SendCustomLog("FirmwareUpdate", msg);
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
