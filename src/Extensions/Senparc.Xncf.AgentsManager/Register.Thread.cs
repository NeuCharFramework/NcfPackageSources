using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Threads;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
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
            // Thread 1: 清理运行超时的未完成任务（每 60 秒执行一次）
            xncfThreadBuilder.AddThreadInfo(new ThreadInfo(
                name: "Agents 定时清理未完成任务",
                intervalTime: TimeSpan.FromSeconds(60),
                task: async (app, threadInfo) =>
                {
                    try
                    {
                        threadInfo.RecordStory("Agents 未完成任务清理开始");

                        using var scope = app.ApplicationServices.CreateScope();
                        var serviceProvider = scope.ServiceProvider;
                        var chatTaskService = serviceProvider.GetService<ChatTaskService>();
                        await chatTaskService.CloseUnfinishedTasksAsync(SystemTime.Now.DateTime.AddDays(-3));
                    }
                    catch (Exception ex)
                    {
                        SenparcTrace.SendCustomLog("AgentsManager", $"清理未完成任务异常：{ex.Message}\r\n{ex.StackTrace}");
                    }
                    finally
                    {
                        threadInfo.RecordStory("Agents 未完成任务清理结束");
                    }
                },
                exceptionHandler: ex =>
                {
                    SenparcTrace.SendCustomLog("AgentsManager", $"清理任务线程异常：{ex.Message}\r\n{ex.StackTrace}\r\n{ex.InnerException?.StackTrace}");
                    return Task.CompletedTask;
                }));

            // Thread 2: 定时任务调度器（每分钟检查一次，触发到期的定时任务）
            xncfThreadBuilder.AddThreadInfo(new ThreadInfo(
                name: "Agents 定时任务调度器",
                intervalTime: TimeSpan.FromMinutes(1),
                task: async (app, threadInfo) =>
                {
                    try
                    {
                        threadInfo.RecordStory("Agents 定时任务调度检测开始");

                        using var scope = app.ApplicationServices.CreateScope();
                        var serviceProvider = scope.ServiceProvider;
                        var chatTaskService = serviceProvider.GetService<ChatTaskService>();
                        var chatGroupService = serviceProvider.GetService<ChatGroupService>();

                        var now = SystemTime.Now.DateTime;

                        // 找出所有标记为定时任务且已完成（Finished 或 Cancelled）的任务
                        var finishedScheduledTasks = await chatTaskService.GetFullListAsync(
                            z => z.IsScheduled && (z.Status == ChatTask_Status.Finished || z.Status == ChatTask_Status.Cancelled),
                            z => z.EndTime,
                            Ncf.Core.Enums.OrderingType.Descending);

                        if (finishedScheduledTasks == null || finishedScheduledTasks.Count == 0)
                        {
                            threadInfo.RecordStory("无待调度的定时任务");
                            return;
                        }

                        // 找出所有当前仍在等待/运行中的定时任务（避免重复触发）
                        var pendingScheduledTasks = await chatTaskService.GetFullListAsync(
                            z => z.IsScheduled && (z.Status == ChatTask_Status.Waiting || z.Status == ChatTask_Status.Chatting || z.Status == ChatTask_Status.Paused));
                        var pendingGroupIds = new HashSet<int>(pendingScheduledTasks.Select(t => t.ChatGroupId));

                        var triggeredCount = 0;
                        var processedGroupIds = new HashSet<int>();

                        // 按 ChatGroupId 分组，每个 Group 最多只触发一次
                        var groupedTasks = finishedScheduledTasks
                            .GroupBy(t => t.ChatGroupId)
                            .ToList();

                        foreach (var groupTasks in groupedTasks)
                        {
                            var groupId = groupTasks.Key;

                            // 如果该 Group 已有等待/运行中的定时任务，跳过
                            if (pendingGroupIds.Contains(groupId))
                            {
                                continue;
                            }

                            // 取该组最近一次完成的定时任务
                            var latestTask = groupTasks.First(); // 已按 EndTime 降序排列

                            // 计算下次运行时间
                            var nextRunTime = ComputeNextRunTime(latestTask, latestTask.EndTime);
                            if (nextRunTime > now)
                            {
                                // 还未到触发时间
                                continue;
                            }

                            // 触发新一轮任务
                            try
                            {
                                var request = new ChatGroup_RunGroupRequest
                                {
                                    Name = $"{latestTask.Name} (定时 {now:MM-dd HH:mm})",
                                    ChatGroupId = latestTask.ChatGroupId,
                                    AiModelId = latestTask.AiModelId,
                                    PromptCommand = latestTask.PromptCommand,
                                    Description = latestTask.Description,
                                    Personality = latestTask.IsPersonality,
                                    HookPlatform = latestTask.HookPlatform,
                                    HookParameter = latestTask.HookPlatformParameter,
                                    IsScheduled = true,
                                    ScheduleType = latestTask.ScheduleType,
                                    ScheduleIntervalMinutes = latestTask.ScheduleIntervalMinutes
                                };

                                await chatGroupService.RunChatGroupInThread(request);
                                triggeredCount++;
                                processedGroupIds.Add(groupId);
                                SenparcTrace.SendCustomLog("AgentsManager.Scheduler", $"已触发定时任务：Group={groupId}, Task={latestTask.Name}");
                            }
                            catch (Exception ex)
                            {
                                SenparcTrace.SendCustomLog("AgentsManager.Scheduler", $"触发定时任务失败：Group={groupId}, Error={ex.Message}");
                            }
                        }

                        threadInfo.RecordStory($"Agents 定时任务调度检测结束，本轮触发 {triggeredCount} 个任务");
                    }
                    catch (Exception ex)
                    {
                        SenparcTrace.SendCustomLog("AgentsManager", $"定时任务调度器异常：{ex.Message}\r\n{ex.StackTrace}");
                    }
                },
                exceptionHandler: ex =>
                {
                    SenparcTrace.SendCustomLog("AgentsManager", $"定时任务调度器线程异常：{ex.Message}\r\n{ex.StackTrace}\r\n{ex.InnerException?.StackTrace}");
                    return Task.CompletedTask;
                }));
        }

        /// <summary>
        /// 根据最后完成时间和调度类型，计算下一次触发时间。
        /// </summary>
        private static DateTime ComputeNextRunTime(ChatTask task, DateTime lastEndTime)
        {
            switch (task.ScheduleType)
            {
                case ScheduleType.Interval:
                    var intervalMinutes = task.ScheduleIntervalMinutes ?? 60;
                    return lastEndTime.AddMinutes(intervalMinutes);

                case ScheduleType.Daily:
                    // 每天同一时刻
                    var nextDay = lastEndTime.Date.AddDays(1).Add(lastEndTime.TimeOfDay);
                    return nextDay;

                case ScheduleType.Weekly:
                    // 下一个指定星期几（1=Monday … 7=Sunday）
                    var targetDow = task.ScheduleIntervalMinutes ?? 1; // 1=Monday
                    var dotNetDow = targetDow == 7 ? DayOfWeek.Sunday : (DayOfWeek)targetDow; // 1→Mon…6→Sat
                    var current = lastEndTime.Date.AddDays(1);
                    while ((int)current.DayOfWeek != (int)dotNetDow)
                    {
                        current = current.AddDays(1);
                    }
                    return current.Add(lastEndTime.TimeOfDay);

                case ScheduleType.Monthly:
                    // 下个月的指定日期
                    var targetDay = task.ScheduleIntervalMinutes ?? 1;
                    var nextMonth = new DateTime(lastEndTime.Year, lastEndTime.Month, 1).AddMonths(1);
                    var maxDay = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
                    return new DateTime(nextMonth.Year, nextMonth.Month, Math.Min(targetDay, maxDay)).Add(lastEndTime.TimeOfDay);

                default:
                    return lastEndTime.AddHours(1);
            }
        }
    }
}

