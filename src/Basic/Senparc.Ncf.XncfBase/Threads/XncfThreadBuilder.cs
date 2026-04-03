using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.Trace;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.Threads
{
    //TODO: Thread backup and background operations need to consider tenant issues

    /// <summary>
    ///XNCF Thread module, thread configuration
    /// </summary>
    public class XncfThreadBuilder
    {
        private List<ThreadInfo> _threadInfoList = new List<ThreadInfo>();
        public void AddThreadInfo(ThreadInfo threadInfo)
        {
            _threadInfoList.Add(threadInfo);
        }

        internal void Build(IApplicationBuilder app, IXncfRegister register)
        {
            var threadRegister = register as IXncfThread;
            if (threadRegister == null)
            {
                return;
            }

            //return;//TODO: Background threads will not be enabled until multi-tenancy is completed. The corresponding relationship between threads and tenants needs to be resolved.

            var i = 0;
            //Traverse all thread configurations within a single XNCF
            foreach (var threadInfo in _threadInfoList)
            {
                if (threadInfo.Task == null)
                {
                    continue;
                }
                try
                {
                    i++;
                    //Define thread
                    Thread thread = new Thread(async () =>
                    {
                        SenparcTrace.SendCustomLog("启动线程", $"{register.Name}-{threadInfo.Name}");
                        await Task.Delay(TimeSpan.FromSeconds(i));
                        while (true)
                        {
                            try
                            {
                                await threadInfo.Task.Invoke(app, threadInfo);
                                // It is recommended that developers handle exceptions within threads internally
                            }
                            catch (Exception ex)
                            {
                                if (threadInfo.ExceptionHandler != null)
                                {
                                    await threadInfo.ExceptionHandler.Invoke(ex);
                                }
                                else
                                {
                                    SenparcTrace.BaseExceptionLog(ex);
                                }
                            }
                            finally {
                                //delay
                                await Task.Delay(threadInfo.IntervalTime);
                            }
                        }
                    });
                    thread.Name = $"{register.Uid}-{threadInfo.Name ?? Guid.NewGuid().ToString()}";
                    thread.Start();//start up
                    Register.ThreadCollection[threadInfo] = thread;
                }
                catch (Exception ex)
                {
                    SenparcTrace.BaseExceptionLog(ex);
                }
            }
        }
    }
}
