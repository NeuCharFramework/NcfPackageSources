/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfThreadBuilder.cs
    File description: XncfThreadBuilder related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260813
    Change description: v0.25.0-preview7 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

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
    /// <summary>
    /// Collects module background-thread definitions and builds their guarded
    /// application-lifetime loops.
    /// </summary>
    public class XncfThreadBuilder
    {
        private List<ThreadInfo> _threadInfoList = new List<ThreadInfo>();
        /// <summary>
        /// Adds a thread definition to the module's pending thread collection.
        /// </summary>
        /// <param name="threadInfo">The thread definition to register.</param>
        public void AddThreadInfo(ThreadInfo threadInfo)
        {
            _threadInfoList.Add(threadInfo);
        }

        /// <summary>
        /// Builds and starts all configured background threads for a module registration.
        /// Each thread observes application shutdown and routes failures to its handler or trace log.
        /// </summary>
        /// <param name="app">The application builder providing services and lifetime tokens.</param>
        /// <param name="register">The module registration owning the thread definitions.</param>
        internal void Build(IApplicationBuilder app, IXncfRegister register)
        {
            var threadRegister = register as IXncfThread;
            if (threadRegister == null)
            {
                return;
            }

            //Legacy comment translated to English.

            var i = 0;
            //Legacy comment translated to English.
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

                    //Legacy comment translated to English.
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
                                    //Legacy comment translated to English.
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
                                    //Legacy comment translated to English.
                                    if (!stoppingToken.IsCancellationRequested)
                                    {
                                        await Task.Delay(threadInfo.IntervalTime, stoppingToken).ConfigureAwait(false);
                                    }
                                }
                            }
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            //Legacy comment translated to English.
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
                    thread.Start();//start
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
