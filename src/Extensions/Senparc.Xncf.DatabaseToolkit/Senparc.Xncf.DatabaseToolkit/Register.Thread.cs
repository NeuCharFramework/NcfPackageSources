using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Threads;
using Senparc.Xncf.DatabaseToolkit.OHS.Local.AppService;
using Senparc.Xncf.DatabaseToolkit.OHS.Local.PL;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DatabaseToolkit
{
    public partial class Register : IXncfThread
    {
        public void ThreadConfig(XncfThreadBuilder xncfThreadBuilder)
        {
            // TODO: Specialize in database and responsible for registration of scheduled projects

            //TOOD: Different tenants need to be distinguished
            DateTime lastAlertTime = DateTime.MinValue;

            xncfThreadBuilder.AddThreadInfo(new Ncf.XncfBase.Threads.ThreadInfo(
                name: "定时备份",
                intervalTime: TimeSpan.FromSeconds(30),
                task: async (app, threadInfo) =>
                {
                    try
                    {
                        //SenparcTrace.SendCustomLog("Execute debugging", "DatabaseToolkit.Register.ThreadConfig");
                        threadInfo.RecordStory("开始检测并备份");

                        using (var scope = app.ApplicationServices.CreateScope())
                        {
                            var serviceProvider = scope.ServiceProvider;

                            //Check whether the current module is available
                            XncfRegisterManager xncfRegisterManager = new XncfRegisterManager(serviceProvider);
                            var xncfIsValiable = await xncfRegisterManager.CheckXncfAvailable(this);
                            if (!xncfIsValiable)
                            {
                                //Only prompt once at the same time
                                if (SystemTime.NowDiff(lastAlertTime) > TimeSpan.FromMinutes(10))
                                {
                                    lastAlertTime = SystemTime.Now.DateTime;
                                    var msg = $"[{this.MenuName}] 模块当前不可用或未启用，跳过数据库自动备份轮询";
                                    SenparcTrace.SendCustomLog("数据库自动备份", msg);
                                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                                    Console.WriteLine($"提醒：{msg}");
                                    Console.ResetColor();
                                }

                                return;
                            }

                            //Initialize database backup method
                            DatabaseBackupAppService backupDatabase = serviceProvider.GetService<DatabaseBackupAppService>();
                            //Initialization parameters
                            var backupRequest = new DatabaseBackup_BackupRequest();
                            var dbConfigService = serviceProvider.GetService<ServiceBase<DbConfig>>();
                            var dbConfig = await dbConfigService.GetObjectAsync(z => true);
                            var stopBackup = false;
                            try
                            {
                                if (dbConfig != null && dbConfig.BackupCycleMinutes > 0 && !dbConfig.BackupPath.IsNullOrEmpty())
                                {
                                    if (SystemTime.NowDiff(dbConfig.LastBackupTime) > TimeSpan.FromMinutes(dbConfig.BackupCycleMinutes))
                                    {
                                        backupRequest.Path = dbConfig.BackupPath;
                                        //await backupParam.LoadData(serviceProvider);
                                        //threadInfo.RecordStory("Complete loading of backup setting data");
                                    }
                                    else
                                    {
                                        stopBackup = true;
                                    }
                                }
                                else
                                {
                                    threadInfo.RecordStory("不需要备份，或没有设置备份周期/路径，已忽略本次备份计划");
                                    stopBackup = true;//No backup required, or no settings, return
                                }
                            }
                            catch (Exception ex)
                            {
                                threadInfo.RecordStory($@"遇到异常，可能未配置数据库，已忽略本次备份计划。如需启动，请更新此模块到最新版本。
异常信息：{ex.Message}
{ex.StackTrace}");
                                stopBackup = true;//There may be no configuration database, return
                            }

                            if (stopBackup)
                            {
                                return;
                            }


                            //Execute backup method
                            threadInfo.RecordStory("备份开始：" + backupRequest.Path);
                            var result = await backupDatabase.Backup(backupRequest);
                            if (result.Success == false)
                            {
                                threadInfo.RecordStory("执行备份发生异常：" + result.Data);
                                throw new Exception("执行备份发生异常");
                            }

                            dbConfig.RecordBackupTime();
                            await dbConfigService.SaveObjectAsync(dbConfig);

                            threadInfo.RecordStory("完成数据库自动备份：" + result.Data);
                            SenparcTrace.SendCustomLog("完成数据库自动备份", backupRequest.Path);
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
                    SenparcTrace.SendCustomLog("DatabaseToolkit", $@"{ex.Message}
{ex.StackTrace}
{ex.InnerException?.StackTrace}");
                    return Task.CompletedTask;
                }));
        }
    }
}
