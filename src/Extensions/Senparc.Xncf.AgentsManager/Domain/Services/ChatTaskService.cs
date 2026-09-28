/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatTaskService.cs
    文件功能描述：ChatTaskService 服务逻辑
    
    
    创建标识：Senparc - 20241017
    
    修改标识：Senparc - 20260701
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260704
    修改描述：v0.11.0-preview2 新增 ChatTask 归档能力并完善多数据库迁移支持

----------------------------------------------------------------*/

using Senparc.CO2NET.Trace;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Domain.Models.Usage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class ChatTaskService : ServiceBase<ChatTask>
    {
        private readonly ChatTaskCancellationRegistry _cancellationRegistry;
        private readonly HumanInTheLoopRequestStore _humanInTheLoopRequestStore;

        public ChatTaskService(
            IRepositoryBase<ChatTask> repo,
            IServiceProvider serviceProvider,
            ChatTaskCancellationRegistry cancellationRegistry,
            HumanInTheLoopRequestStore humanInTheLoopRequestStore) : base(repo, serviceProvider)
        {
            _cancellationRegistry = cancellationRegistry;
            _humanInTheLoopRequestStore = humanInTheLoopRequestStore;
        }

        /// <summary>
        /// 获取缓存中记录正在运行的 ChatTask 的 Key
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public string GetChatTaskRunCacheKey(int taskId)
        {
            return $"ChatTask-Running:{taskId}";
        }

        public async Task<ChatTask> CreateTask(ChatTaskDto chatTaskDto)
        {
            var chatTask = new ChatTask(chatTaskDto);

            //TODO:需要缓存，以便快速读取

            await base.SaveObjectAsync(chatTask);

            return chatTask;
            //return base.Mapping<ChatTaskDto>(chatTask);
        }

        public async Task SetStatus(ChatTask_Status status, ChatTask chatTask)
        {
            chatTask.ChangeStatus(status);
            await base.SaveObjectAsync(chatTask);

            await TryFinishChatGroupIfAllTasksSettledAsync(chatTask);
        }

        /// <summary>
        /// 当任务进入终态（完成/取消/失败）时，检查同一 ChatGroup 下是否还有未结束的任务；
        /// 若没有，则将 ChatGroup 状态由 Running 置为 Finished。
        /// 群组状态同步属于尽力而为：失败只记录日志，不影响任务状态更新主流程。
        /// </summary>
        private async Task TryFinishChatGroupIfAllTasksSettledAsync(ChatTask chatTask)
        {
            if (chatTask.ChatGroupId <= 0)
            {
                return;
            }

            if (chatTask.Status != ChatTask_Status.Finished
                && chatTask.Status != ChatTask_Status.Cancelled
                && chatTask.Status != ChatTask_Status.Failed)
            {
                return;
            }

            var unsettledCount = await base.GetCountAsync(z => z.ChatGroupId == chatTask.ChatGroupId
                && (z.Status == ChatTask_Status.Waiting
                    || z.Status == ChatTask_Status.Chatting
                    || z.Status == ChatTask_Status.Paused));
            if (unsettledCount > 0)
            {
                return;
            }

            try
            {
                var chatGroupService = _serviceProvider.GetRequiredService<ChatGroupService>();
                var chatGroup = await chatGroupService.GetObjectAsync(z => z.Id == chatTask.ChatGroupId);
                if (chatGroup != null && chatGroup.State == ChatGroupState.Running)
                {
                    chatGroup.Finish();
                    await chatGroupService.SaveObjectAsync(chatGroup);
                }
            }
            catch (Exception ex)
            {
                SenparcTrace.BaseExceptionLog(ex);
            }
        }

        public async Task SetArchiveStatus(ChatTask chatTask, bool isArchived)
        {
            if (chatTask == null)
            {
                return;
            }

            chatTask.SetArchived(isArchived);
            await base.SaveObjectAsync(chatTask);
        }

        /// <summary>
        /// 关闭长时间未结束的孤儿任务（Chatting 与 Paused）。
        /// 仍在运行的任务（存在进程内取消源）会被跳过，避免误杀正在执行或正在等待人工审批的任务；
        /// 被关闭任务的 Human-in-the-Loop 待处理请求会一并取消。
        /// </summary>
        /// <param name="beforeStartDateTime">只筛选在此时间之前开始的任务</param>
        /// <returns></returns>
        public async Task CloseUnfinishedTasksAsync(DateTime beforeStartDateTime)
        {
            var unfinishTasks = await base.GetObjectListAsync(0, 0,
                z => z.StartTime < beforeStartDateTime
                     && (z.Status == ChatTask_Status.Chatting || z.Status == ChatTask_Status.Paused),
                z => z.Id, Ncf.Core.Enums.OrderingType.Ascending);
            foreach (var unfinishedTask in unfinishTasks)
            {
                if (_cancellationRegistry.IsRegistered(unfinishedTask.Id))
                {
                    // 当前进程仍有该任务的运行上下文（可能正在等待人工审批），不处理。
                    continue;
                }

                SenparcTrace.SendCustomLog($"处理未完成任务({unfinishedTask.Id})", $"任务：{unfinishedTask.Name}，开始时间：{unfinishedTask.StartTime}，状态：{unfinishedTask.Status}");
                _humanInTheLoopRequestStore.CancelForTask(unfinishedTask.Id);
                unfinishedTask.ChangeStatus(ChatTask_Status.Cancelled);
                await base.SaveObjectAsync(unfinishedTask);
                SenparcTrace.SendCustomLog($"处理未完成任务({unfinishedTask.Id})", $"处理完成，当前状态：{unfinishedTask.Status}");
            }
        }

        public async Task UpdateUsageAggregateAsync(ChatTask chatTask, ChatUsageSnapshot usageSnapshot)
        {
            if (chatTask == null || usageSnapshot == null)
            {
                return;
            }

            var aggregate = ChatUsageRemarkCodec.DecodeAggregateOrDefault(chatTask.AdminRemark);
            aggregate.Merge(usageSnapshot);
            chatTask.AdminRemark = ChatUsageRemarkCodec.EncodeAggregate(aggregate);
            await base.SaveObjectAsync(chatTask);
        }
    }
}
