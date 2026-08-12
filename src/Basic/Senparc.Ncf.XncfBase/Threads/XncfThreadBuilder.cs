/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XncfThreadBuilder.cs
    文件功能描述：XncfThreadBuilder 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

            //return;//TODO:多租户完成之前暂时不启用后台线程，需要解决线程和租户的对应关系

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
                    var initialDelay = TimeSpan.FromSeconds(i);
                    var applicationLifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();
                    var stoppingToken = applicationLifetime?.ApplicationStopping ?? CancellationToken.None;
                    threadInfo.StoppingToken = stoppingToken;

                    //定义线程
                    Thread thread = new Thread(async () =>
                    {
                        try
                        {
                            SenparcTrace.SendCustomLog("启动线程", $"{register.Name}-{threadInfo.Name}");
                            await Task.Delay(initialDelay, stoppingToken).ConfigureAwait(false);
                            while (!stoppingToken.IsCancellationRequested)
                            {
                                try
                                {
                                    await threadInfo.Task.Invoke(app, threadInfo).ConfigureAwait(false);
                                    // 建议开发者自己在内部做好线程内的异常处理
                                }
                                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                                {
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    if (threadInfo.ExceptionHandler != null)
                                    {
                                        await threadInfo.ExceptionHandler.Invoke(ex).ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        SenparcTrace.BaseExceptionLog(ex);
                                    }
                                }
                                finally
                                {
                                    //进行延迟
                                    if (!stoppingToken.IsCancellationRequested)
                                    {
                                        await Task.Delay(threadInfo.IntervalTime, stoppingToken).ConfigureAwait(false);
                                    }
                                }
                            }
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            // 应用停止时取消初始/轮询等待是预期行为，无需记录为线程异常。
                        }
                        catch (Exception ex)
                        {
                            if (threadInfo.ExceptionHandler != null)
                            {
                                await threadInfo.ExceptionHandler.Invoke(ex).ConfigureAwait(false);
                            }
                            else
                            {
                                SenparcTrace.BaseExceptionLog(ex);
                            }
                        }
                    });
                    thread.Name = $"{register.Uid}-{threadInfo.Name ?? Guid.NewGuid().ToString()}";
                    thread.IsBackground = true;
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
