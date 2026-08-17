/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatTaskAppService.cs
    文件功能描述：ChatTaskAppService 相关实现
    
    
    创建标识：Senparc - 20241017
    
    修改标识：Senparc - 20260704
    修改描述：v0.11.0-preview2 新增 ChatTask 归档能力并完善多数据库迁移支持

    修改标识：Senparc - 20260705
    修改描述：v0.11.1-preview3 重构系统配置初始化与更新流程并统一模型处理

    修改标识：Senparc - 20260705
    修改描述：v0.11.2-preview4 重构系统配置初始化与更新流程并统一模型处理

    修改标识：Senparc - 20260817
    修改描述：v0.16.0 支持 Human-in-the-Loop 人工审批与人类参与者执行策略

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
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.WorkContext.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    [ApiAuthorize]
    public class ChatTaskAppService : AppServiceBase
    {
        private readonly ChatTaskService _chatTaskService;
        private readonly ChatGroupService _chatGroupService;
        private readonly HumanInTheLoopRequestStore _humanInTheLoopRequestStore;
        private readonly AgentsManagerHumanInteractionService _humanInteractionService;

        public ChatTaskAppService(
            IServiceProvider serviceProvider,
            ChatTaskService chatTaskService,
            ChatGroupService chatGroupService,
            HumanInTheLoopRequestStore humanInTheLoopRequestStore,
            AgentsManagerHumanInteractionService humanInteractionService) : base(serviceProvider)
        {
            _chatTaskService = chatTaskService;
            _chatGroupService = chatGroupService;
            _humanInTheLoopRequestStore = humanInTheLoopRequestStore;
            _humanInteractionService = humanInteractionService;
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
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
                      // 归档筛选：0=活动（未归档），1=已归档，2=全部
                      seh.ValueCompare
                          .AndAlso(archiveScope == 0, z => z.IsArchived == false)
                          .AndAlso(archiveScope == 1, z => z.IsArchived == true);
                      //增加模糊搜索任务
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
        public async Task<AppResponseBase<List<HumanInTheLoopRequestDto>>> GetHumanRequests(int chatTaskId)
        {
            return await this.GetResponseAsync<List<HumanInTheLoopRequestDto>>(async (response, logger) =>
            {
                return _humanInTheLoopRequestStore.GetPending(chatTaskId).ToList();
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
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
        public async Task<AppResponseBase<string>> ForceStop(int id)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var result = await ForceStopInternalAsync(new List<int> { id });
                return result;
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> ForceStopBatch([FromBody] List<int> ids)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var result = await ForceStopInternalAsync(ids);
                return result;
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> Delete(int id)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var result = await DeleteInternalAsync(new List<int> { id });
                return result;
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> DeleteBatch([FromBody] List<int> ids)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var result = await DeleteInternalAsync(ids);
                return result;
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
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

                    var runRequest = new ChatGroup_RunGroupRequest
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
                changed++;
            }

            return $"强制停止完成：成功 {changed} 条，跳过 {skipped} 条";
        }

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
                await _chatTaskService.DeleteObjectAsync(task);
            }

            return $"删除任务完成：成功 {taskList.Count} 条";
        }
    }

    public sealed class HumanMessageRequest
    {
        public string RequestId { get; set; }

        public string Input { get; set; }
    }
}
