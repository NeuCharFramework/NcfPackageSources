/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatTaskService.cs
    File description: Related implementation.

    Created by: Senparc - 20241017

    Modified by: Senparc - 20260701
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260704
    Change description: v0.11.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

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

        /// <summary>
        /// Initializes the ChatTask persistence service and cancellation/Human-in-the-Loop collaborators.
        /// </summary>
        /// <param name="repo">The ChatTask repository.</param>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        /// <param name="cancellationRegistry">The running-task cancellation registry.</param>
        /// <param name="humanInTheLoopRequestStore">The pending human-request store.</param>
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
        /// Builds the cache key used to track a running chat task.
        /// </summary>
        /// <param name="taskId">The chat-task identifier.</param>
        /// <returns>The stable running-task cache key.</returns>
        public string GetChatTaskRunCacheKey(int taskId)
        {
            return $"ChatTask-Running:{taskId}";
        }

        /// <summary>
        /// Creates and persists a chat task from a DTO.
        /// </summary>
        /// <param name="chatTaskDto">The task values to persist.</param>
        /// <returns>The persisted chat task.</returns>
        public async Task<ChatTask> CreateTask(ChatTaskDto chatTaskDto)
        {
            var chatTask = new ChatTask(chatTaskDto);

            await base.SaveObjectAsync(chatTask);

            return chatTask;
            //return base.Mapping<ChatTaskDto>(chatTask);
        }

        /// <summary>
        /// Changes a task status, persists it, and closes its group when all group tasks settle.
        /// </summary>
        /// <param name="status">The new task status.</param>
        /// <param name="chatTask">The task to update.</param>
        public async Task SetStatus(ChatTask_Status status, ChatTask chatTask)
        {
            chatTask.ChangeStatus(status);
            await base.SaveObjectAsync(chatTask);

            await TryFinishChatGroupIfAllTasksSettledAsync(chatTask);
        }

        /// <summary>
        /// Finishes the owning group when the changed task is terminal and no sibling task remains active.
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

        /// <summary>
        /// Changes the archive state of a task and persists the result.
        /// </summary>
        /// <param name="chatTask">The task to update.</param>
        /// <param name="isArchived">Whether the task should be archived.</param>
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
        /// Cancels unfinished chatting or paused tasks that began before a cutoff,
        /// excluding tasks currently registered for active cancellation.
        /// </summary>
        /// <param name="beforeStartDateTime">The start-time cutoff.</param>
        /// <returns>A task that completes after all eligible tasks are cancelled.</returns>
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
                    continue;
                }

                SenparcTrace.SendCustomLog($"处理未完成任务({unfinishedTask.Id})", $"任务：{unfinishedTask.Name}，开始时间：{unfinishedTask.StartTime}，状态：{unfinishedTask.Status}");
                _humanInTheLoopRequestStore.CancelForTask(unfinishedTask.Id);
                unfinishedTask.ChangeStatus(ChatTask_Status.Cancelled);
                await base.SaveObjectAsync(unfinishedTask);
                SenparcTrace.SendCustomLog($"处理未完成任务({unfinishedTask.Id})", $"处理完成，当前状态：{unfinishedTask.Status}");
            }
        }

        /// <summary>
        /// Merges one usage snapshot into the compact aggregate stored in the task's admin remark.
        /// </summary>
        /// <param name="chatTask">The task whose aggregate should be updated.</param>
        /// <param name="usageSnapshot">The usage snapshot to merge.</param>
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
