/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.Thread.cs
    文件功能描述：Register.Thread 相关实现
    
    
    创建标识：Senparc - 20241216
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
            // 定时清理孤儿任务：长时间停留在 Chatting/Paused 且当前进程没有运行上下文的任务。
            // CloseUnfinishedTasksAsync 会跳过仍在运行（含等待人工审批）的任务，并联动取消 HIL 请求。
            xncfThreadBuilder.AddThreadInfo(new ThreadInfo(
                name: "Agents 定时清理未完成任务",
                intervalTime: TimeSpan.FromMinutes(10),
                task: async (app, threadInfo) =>
                {
                    try
                    {
                        // 多租户模式下，根线程作用域没有租户上下文，直接清理会跨租户误操作，因此跳过。
                        if (Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.EnableMultiTenant)
                        {
                            threadInfo.RecordStory("多租户模式，跳过孤儿任务检测");
                            return;
                        }

                        threadInfo.RecordStory("Agents 开始检测孤儿任务");

                        using (var scope = app.ApplicationServices.CreateScope())
                        {
                            var serviceProvider = scope.ServiceProvider;

                            var chatTaskService = serviceProvider.GetRequiredService<ChatTaskService>();
                            await chatTaskService.CloseUnfinishedTasksAsync(SystemTime.Now.DateTime.AddDays(-1));
                        }
                    }
                    finally
                    {
                        threadInfo.RecordStory("孤儿任务检测结束");
                    }
                },
                exceptionHandler: ex =>
                {
                    SenparcTrace.SendCustomLog("AgentsManager.OrphanTaskCleanup", $@"{ex.Message}
{ex.StackTrace}");
                    return Task.CompletedTask;
                }));
        }
    }
}
