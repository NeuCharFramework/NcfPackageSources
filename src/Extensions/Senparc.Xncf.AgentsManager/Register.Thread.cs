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
            //TOOD: Different tenants need to be distinguished
            DateTime lastAlertTime = DateTime.MinValue;
            //TODO: Debugging multi-tenancy, temporarily disabled

//            xncfThreadBuilder.AddThreadInfo(new Ncf.XncfBase.Threads.ThreadInfo(
//                name: "Agents regularly clean up unfinished tasks",
//                intervalTime: TimeSpan.FromSeconds(60),
//                task: async (app, threadInfo) =>
//                {
//                    try
//                    {
//                        //SenparcTrace.SendCustomLog("Execute debugging", "DatabaseToolkit.Register.ThreadConfig");
//                        threadInfo.RecordStory("Agents task started to detect and complete");

//                        using (var scope = app.ApplicationServices.CreateScope())
//                        {
//                            var serviceProvider = scope.ServiceProvider;

//                            var chatTaskService = serviceProvider.GetService<ChatTaskService>();
//                            await chatTaskService.CloseUnfinishedTasksAsync(SystemTime.Now.DateTime.AddDays(-3));
//                        }
//                    }
//                    catch (NcfModuleException ex)
//                    {
//                        throw;
//                    }
//                    catch
//                    {
//                        throw;
//                    }
//                    finally
//                    {
//                        threadInfo.RecordStory("Detection and backup completed");
//                    }
//                },
//                exceptionHandler: ex =>
//                {
//                    SenparcTrace.SendCustomLog("AgentsManager", $@"{ex.Message}
//{ex.StackTrace}
//{ex.InnerException?.StackTrace}");
//                    return Task.CompletedTask;
//                }));
        }
    }
}
