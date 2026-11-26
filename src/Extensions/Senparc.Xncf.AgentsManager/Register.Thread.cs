/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Thread.cs
    File description: Register.Thread related implementation

    Created by: Senparc - 20241216

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

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
        /// <summary>
        /// Registers the AgentsManager background task that periodically closes orphaned
        /// chat tasks when the application is running in single-tenant mode.
        /// </summary>
        /// <param name="xncfThreadBuilder">The thread builder receiving the scheduled task definition.</param>
        /// <exception cref="ArgumentNullException">Thrown by the builder when the supplied builder is <see langword="null"/>.</exception>
        public void ThreadConfig(XncfThreadBuilder xncfThreadBuilder)
        {
            xncfThreadBuilder.AddThreadInfo(new ThreadInfo(
                name: "Agents 定时清理未完成任务",
                intervalTime: TimeSpan.FromMinutes(10),
                task: async (app, threadInfo) =>
                {
                    try
                    {
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
