/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatTaskAppService.cs
    File description: ChatTaskAppService related implementation
    -

    Created by: Senparc - 20241017

    Modified by: Senparc - 20260704
    Change description: v0.11.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.11.1-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.11.2-preview4 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Application.Dtos;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.WorkContext.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    [ApiAuthorize]
    /// <summary>
    /// Exposes authenticated administration operations for listing, reviewing, controlling,
    /// and deleting agent chat tasks.
    /// </summary>
    public class ChatTaskAppService : AppServiceBase
    {
        private readonly ChatTaskService _chatTaskService;
        private readonly ChatGroupService _chatGroupService;
        private readonly HumanInTheLoopRequestStore _humanInTheLoopRequestStore;
        private readonly AgentsManagerHumanInteractionService _humanInteractionService;
        private readonly ChatTaskCancellationRegistry _cancellationRegistry;

        /// <summary>
        /// Initializes the application service and its task-control collaborators.
        /// </summary>
        /// <param name="serviceProvider">The dependency injection provider used by the base application service.</param>
        /// <param name="chatTaskService">The service used to query and persist chat tasks.</param>
        /// <param name="chatGroupService">The service used to start the task's associated chat group.</param>
        /// <param name="humanInTheLoopRequestStore">The store used to inspect and cancel pending human requests.</param>
        /// <param name="humanInteractionService">The service used to resolve human-in-the-loop decisions and replies.</param>
        /// <param name="cancellationRegistry">The registry used to signal cancellation to active task executions.</param>
        public ChatTaskAppService(
            IServiceProvider serviceProvider,
            ChatTaskService chatTaskService,
            ChatGroupService chatGroupService,
            HumanInTheLoopRequestStore humanInTheLoopRequestStore,
            AgentsManagerHumanInteractionService humanInteractionService,
            ChatTaskCancellationRegistry cancellationRegistry) : base(serviceProvider)
        {
            _chatTaskService = chatTaskService;
            _chatGroupService = chatGroupService;
            _humanInTheLoopRequestStore = humanInTheLoopRequestStore;
            _humanInteractionService = humanInteractionService;
            _cancellationRegistry = cancellationRegistry;
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        /// <summary>
        /// Queries tasks with optional chat-group, agent-template, name, and archive-state filters.
        /// </summary>
        /// <param name="chatGroupId">Optional chat-group identifier; non-positive values do not filter by group.</param>
        /// <param name="agentTemplateId">Optional agent-template identifier used to derive matching chat groups.</param>
        /// <param name="pageIndex">The zero- or one-based page index expected by the underlying service.</param>
        /// <param name="pageSize">The maximum number of tasks requested from the underlying service.</param>
        /// <param name="filter">Optional case-sensitive or provider-dependent task-name filter.</param>
        /// <param name="archiveScope">Archive selector: <c>0</c> active only, <c>1</c> archived only, other values both.</param>
        /// <returns>An application response containing the mapped task page.</returns>
        public async Task<AppResponseBase<ChatTask_GetListResponse>> GetList(int chatGroupId, int agentTemplateId, int pageIndex, int pageSize, string filter = "", int archiveScope = 2)
        {
            return await this.GetResponseAsync<ChatTask_GetListResponse>(async (response, logger) =>
                  {
                      var chatGroupIdList = new List<int>();
                      if (agentTemplateId > 0)
                      {
                          var agentTemplateService = base.GetRequiredService<AgentTemplateAppService>();
                          var memberService = base.GetRequiredService<ChatGroupMemberService>();
                          var chatGroupList = await memberService.GetFullListAsync(z => z.AgentTemplateId == agentTemplateId);
                           chatGroupIdList = chatGroupList.Select(z => z.ChatGroupId).ToList();

                          //chatTaskIdList = this._chatTaskService.GetFullList(z=> chatGroupIdList.Contains(z.ChatGroupId)).Select
                      }

                      var seh = new SenparcExpressionHelper<ChatTask>();
                      seh.ValueCompare
                          .AndAlso(chatGroupId > 0, z => z.ChatGroupId == chatGroupId)
                          .AndAlso(agentTemplateId > 0, z => chatGroupIdList.Contains(z.ChatGroupId));
                      // Archive scope 0 selects active tasks and scope 1 selects archived tasks.
                      seh.ValueCompare
                          .AndAlso(archiveScope == 0, z => z.IsArchived == false)
                          .AndAlso(archiveScope == 1, z => z.IsArchived == true);
                      // An empty filter keeps the query unfiltered by task name.
                      seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(filter), _ => _.Name.Contains(filter));
                      var where = seh.BuildWhereExpression();

                      var list = await this._chatTaskService.GetObjectListAsync(pageIndex, pageSize, where, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);

                      return new ChatTask_GetListResponse()
                      {
                          ChatTaskList = this._chatTaskService.Mapping<ChatTaskDto>(list)
                      };
                  });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        /// <summary>
        /// Changes the archive flag of one task.
        /// </summary>
        /// <param name="id">The task identifier.</param>
        /// <param name="isArchived">Whether the task should be marked archived.</param>
        /// <returns>An application response containing a localized operation result.</returns>
        public async Task<AppResponseBase<string>> SetArchiveStatus(int id, bool isArchived)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var task = await _chatTaskService.GetObjectAsync(z => z.Id == id);
                if (task == null)
                {
                    return $"任务不存在：{id}";
                }

                await _chatTaskService.SetArchiveStatus(task, isArchived);
                return isArchived ? "归档成功" : "已取消归档";
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        /// <summary>
        /// Loads one task and maps it to the application-service item response.
        /// </summary>
        /// <param name="id">The task identifier.</param>
        /// <returns>The mapped task response; the mapping behavior for a missing task is delegated to the configured mapper.</returns>
        public async Task<AppResponseBase<ChatTask_GetItemResponse>> GetItem(int id)
        {
            return await this.GetResponseAsync<ChatTask_GetItemResponse>(async (response, logger) =>
            {
                var chatTask = await this._chatTaskService.GetObjectAsync(z => z.Id == id);

                return new ChatTask_GetItemResponse()
                {
                    ChatTaskDto = this._chatTaskService.Mapping<ChatTaskDto>(chatTask)
                };
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        /// <summary>
        /// Returns pending human-in-the-loop requests for a task.
        /// </summary>
        /// <param name="chatTaskId">The task identifier whose pending requests should be returned.</param>
        /// <returns>An application response containing a materialized list of pending requests.</returns>
        public async Task<AppResponseBase<List<HumanInTheLoopRequestDto>>> GetHumanRequests(int chatTaskId)
        {
            return await this.GetResponseAsync<List<HumanInTheLoopRequestDto>>(async (response, logger) =>
            {
                return _humanInTheLoopRequestStore.GetPending(chatTaskId).ToList();
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        /// <summary>
        /// Resolves a pending tool-approval request for the current administrator.
        /// </summary>
        /// <param name="requestId">The pending human-interaction request identifier.</param>
        /// <param name="approved">Whether the requested tool action is approved.</param>
        /// <param name="reason">Optional reason to store with the decision.</param>
        /// <param name="input">Optional input associated with the decision.</param>
        /// <returns>An application response containing the localized resolution result.</returns>
        public async Task<AppResponseBase<string>> ResolveHumanRequest(
            string requestId,
            bool approved,
            string reason = null,
            string input = null)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                if (_humanInTheLoopRequestStore.TryGet(requestId, out var pending)
                    && string.Equals(pending.RequestType, "humanTurn", StringComparison.Ordinal))
                {
                    response.Success = false;
                    response.ErrorMessage = "Human 回合必须通过文本回复接口提交，不能使用工具审批接口。";
                    return null;
                }

                var resolution = await _humanInteractionService.ResolveAsync(
                    requestId,
                    GetCurrentAdminUserId(),
                    new HumanInTheLoopDecision(approved, reason, input));
                if (!resolution.Success)
                {
                    response.Success = false;
                    response.ErrorMessage = resolution.Message;
                    return null;
                }

                return approved ? "人工审批已批准" : "人工审批已拒绝";
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        /// <summary>
        /// Submits text to a pending human-turn request and allows the waiting task to continue.
        /// </summary>
        /// <param name="request">The request identifier and non-empty human text.</param>
        /// <returns>An application response containing the localized submission result.</returns>
        public async Task<AppResponseBase<string>> SendHumanMessage([FromBody] HumanMessageRequest request)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Input))
                {
                    response.Success = false;
                    response.ErrorMessage = "Human 回复不能为空";
                    return null;
                }

                if (!_humanInTheLoopRequestStore.TryGet(request.RequestId, out var pending)
                    || !string.Equals(pending.RequestType, "humanTurn", StringComparison.Ordinal))
                {
                    response.Success = false;
                    response.ErrorMessage = "Human 回合不存在、已处理或已失效";
                    return null;
                }

                var resolution = await _humanInteractionService.ResolveAsync(
                    request.RequestId,
                    GetCurrentAdminUserId(),
                    new HumanInTheLoopDecision(true, "Human 文本回复", request.Input.Trim()));
                if (!resolution.Success)
                {
                    response.Success = false;
                    response.ErrorMessage = resolution.Message;
                    return null;
                }

                return "Human 回复已提交，任务继续执行";
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        /// <summary>
        /// Requests cancellation of one active task and releases its pending runtime resources.
        /// </summary>
        /// <param name="id">The task identifier.</param>
        /// <returns>An application response describing the cancellation result.</returns>
        public async Task<AppResponseBase<string>> ForceStop(int id)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var result = await ForceStopInternalAsync(new List<int> { id });
                return result;
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        /// <summary>
        /// Requests cancellation of multiple tasks in one operation.
        /// </summary>
        /// <param name="ids">The task identifiers to cancel; null or empty input is rejected by the operation.</param>
        /// <returns>An application response containing counts of changed and skipped tasks.</returns>
        public async Task<AppResponseBase<string>> ForceStopBatch([FromBody] List<int> ids)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var result = await ForceStopInternalAsync(ids);
                return result;
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        /// <summary>
        /// Deletes one task together with its persisted chat history and runtime state.
        /// </summary>
        /// <param name="id">The task identifier.</param>
        /// <returns>An application response describing the deletion result.</returns>
        public async Task<AppResponseBase<string>> Delete(int id)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var result = await DeleteInternalAsync(new List<int> { id });
                return result;
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        /// <summary>
        /// Deletes multiple tasks and their persisted histories and runtime state.
        /// </summary>
        /// <param name="ids">The task identifiers to delete; null or empty input is rejected by the operation.</param>
        /// <returns>An application response containing the number of deleted tasks.</returns>
        public async Task<AppResponseBase<string>> DeleteBatch([FromBody] List<int> ids)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var result = await DeleteInternalAsync(ids);
                return result;
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        /// <summary>
        /// Starts the eligible tasks in a batch by dispatching their associated chat groups.
        /// </summary>
        /// <param name="ids">The task identifiers to start.</param>
        /// <returns>An application response containing started and skipped counts.</returns>
        public async Task<AppResponseBase<string>> StartBatch([FromBody] List<int> ids)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                if (ids == null || ids.Count == 0)
                {
                    return "未提供任务 ID";
                }

                var idSet = ids.Distinct().ToList();
                var taskList = await _chatTaskService.GetFullListAsync(z => idSet.Contains(z.Id));
                if (taskList == null || taskList.Count == 0)
                {
                    return "未找到可启动的任务";
                }

                var started = 0;
                var skipped = 0;

                foreach (var task in taskList)
                {
                    if (task.ChatGroupId <= 0 || task.PromptCommand.IsNullOrEmpty())
                    {
                        skipped++;
                        continue;
                    }

                    var runRequest = new ChatGroupRunCommand
                    {
                        Name = task.Name,
                        ChatGroupId = task.ChatGroupId,
                        AiModelId = task.AiModelId,
                        PromptCommand = task.PromptCommand,
                        Description = task.Description,
                        Personality = task.IsPersonality,
                        RequireHumanApproval = task.RequireHumanApproval,
                        HumanInTheLoopLevel = task.HumanInTheLoopLevel,
                        PluginToolPermission = task.PluginToolPermission,
                        McpToolPermission = task.McpToolPermission,
                        IncludeHumanParticipant = task.IncludeHumanParticipant,
                        ChatMaxRound = task.ChatMaxRound,
                        HumanRecipientUserId = GetCurrentAdminUserId(),
                        HookPlatform = task.HookPlatform,
                        HookParameter = task.HookPlatformParameter
                    };

                    await _chatGroupService.RunChatGroupInThread(runRequest);
                    started++;
                }

                return $"批量启动完成：成功 {started} 条，跳过 {skipped} 条";
            });
        }

        /// <summary>
        /// Performs the shared cancellation workflow used by single-task and batch endpoints.
        /// </summary>
        /// <param name="ids">The candidate task identifiers.</param>
        /// <returns>A localized result containing changed and skipped counts.</returns>
        private async Task<string> ForceStopInternalAsync(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return "未提供任务 ID";
            }

            var idSet = ids.Distinct().ToList();
            var taskList = await _chatTaskService.GetFullListAsync(z => idSet.Contains(z.Id));
            var cache = base.GetRequiredService<IBaseObjectCacheStrategy>();

            var changed = 0;
            var skipped = 0;

            foreach (var task in taskList)
            {
                if (task.Status == ChatTask_Status.Finished
                    || task.Status == ChatTask_Status.Cancelled
                    || task.Status == ChatTask_Status.Failed)
                {
                    skipped++;
                    continue;
                }

                await _chatTaskService.SetStatus(ChatTask_Status.Cancelled, task);
                _humanInTheLoopRequestStore.CancelForTask(task.Id);
                await cache.RemoveFromCacheAsync(_chatTaskService.GetChatTaskRunCacheKey(task.Id));
                _cancellationRegistry.TryCancel(task.Id);
                changed++;
            }

            return $"强制停止完成：成功 {changed} 条，跳过 {skipped} 条";
        }

        /// <summary>
        /// Reads the current administrator identifier for human-interaction ownership checks.
        /// </summary>
        /// <returns>The administrator identifier as text, or <see langword="null"/> when no usable context exists.</returns>
        private string GetCurrentAdminUserId()
        {
            try
            {
                var context = base.GetService<IAdminWorkContextProvider>()?.GetAdminWorkContext();
                return context?.AdminUserId > 0 ? context.AdminUserId.ToString() : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Performs the shared deletion workflow used by single-task and batch endpoints.
        /// </summary>
        /// <param name="ids">The candidate task identifiers.</param>
        /// <returns>A localized result containing the number of deleted tasks.</returns>
        private async Task<string> DeleteInternalAsync(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return "未提供任务 ID";
            }

            var idSet = ids.Distinct().ToList();
            var taskList = await _chatTaskService.GetFullListAsync(z => idSet.Contains(z.Id));
            var taskIds = taskList.Select(z => z.Id).ToList();
            var historyService = base.GetRequiredService<ChatGroupHistoryService>();
            var cache = base.GetRequiredService<IBaseObjectCacheStrategy>();

            if (taskIds.Count > 0)
            {
                var histories = await historyService.GetFullListAsync(z => taskIds.Contains(z.ChatTaskId));
                foreach (var history in histories)
                {
                    await historyService.DeleteObjectAsync(history);
                }
            }

            foreach (var task in taskList)
            {
                _humanInTheLoopRequestStore.CancelForTask(task.Id);
                await cache.RemoveFromCacheAsync(_chatTaskService.GetChatTaskRunCacheKey(task.Id));
                _cancellationRegistry.TryCancel(task.Id);
                await _chatTaskService.DeleteObjectAsync(task);
            }

            return $"删除任务完成：成功 {taskList.Count} 条";
        }
    }

    /// <summary>
    /// Carries text submitted to a pending human-turn interaction.
    /// </summary>
    public sealed class HumanMessageRequest
    {
        public string RequestId { get; set; }

        public string Input { get; set; }
    }
}
