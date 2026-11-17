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
        ///Legacy comment translated to English.
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

            //Legacy comment translated to English.

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
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
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
                    //Legacy comment translated to English.
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
