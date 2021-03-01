using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.Trace;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.Threads
{
    //TODO: 线程备份及后台操作需要考虑租户问题

    /// <summary>
    /// XNCF Thread 模块，线程配置
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

            return;//TODO:多租户完成之前暂时不启用后台线程，需要解决线程和租户的对应关系

            var i = 0;
            //遍历单个 XNCF 内所有线程配置
            foreach (var threadInfo in _threadInfoList)
            {
                if (threadInfo.Task == null)
                {
                    continue;
                }
                try
                {
                    i++;
                    //定义线程
                    Thread thread = new Thread(async () =>
                    {
                        SenparcTrace.SendCustomLog("启动线程", $"{register.Name}-{threadInfo.Name}");
                        await Task.Delay(TimeSpan.FromSeconds(i));
                        while (true)
                        {
                            try
                            {
                                await threadInfo.Task.Invoke(app, threadInfo);
                                // 建议开发者自己在内部做好线程内的异常处理
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
                                //进行延迟
                                await Task.Delay(threadInfo.IntervalTime);
                            }
                        }
                    });
                    thread.Name = $"{register.Uid}-{threadInfo.Name ?? Guid.NewGuid().ToString()}";
                    thread.Start();//启动
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
