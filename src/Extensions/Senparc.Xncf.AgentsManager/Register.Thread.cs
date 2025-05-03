using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Threads;
using Senparc.Xncf.AgentsManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager
{
    public partial class Register : IXncfThread
    {
        public void ThreadConfig(XncfThreadBuilder xncfThreadBuilder)
        {
            //TOOD: 按照不同租户，需要区分
            DateTime lastAlertTime = DateTime.MinValue;

            xncfThreadBuilder.AddThreadInfo(new Ncf.XncfBase.Threads.ThreadInfo(
                name: "Agents 定时清理未完成任务",
                intervalTime: TimeSpan.FromSeconds(60),
                task: async (app, threadInfo) =>
                {
                    try
                    {
                        return;//TODO: 调试多租户，暂时禁用
                        //SenparcTrace.SendCustomLog("执行调试", "DatabaseToolkit.Register.ThreadConfig");
                        threadInfo.RecordStory("Agents 任务开始检测并");

                        using (var scope = app.ApplicationServices.CreateScope())
                        {
                            var serviceProvider = scope.ServiceProvider;

                            var chatTaskService = serviceProvider.GetService<ChatTaskService>();
                            await chatTaskService.CloseUnfinishedTasksAsync(SystemTime.Now.DateTime.AddDays(-3));
                        }
                    }
                    catch (NcfModuleException ex)
                    {
                        throw;
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        threadInfo.RecordStory("检测并备份结束");
                    }
                },
                exceptionHandler: ex =>
                {
                    SenparcTrace.SendCustomLog("AgentsManager", $@"{ex.Message}
{ex.StackTrace}
{ex.InnerException?.StackTrace}");
                    return Task.CompletedTask;
                }));
        }
    }
}
