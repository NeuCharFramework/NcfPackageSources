using Microsoft.CodeAnalysis.CSharp;
using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    public class ChatGroupAppService : AppServiceBase
    {
        private readonly ChatGroupService _chatGroupService;
        private readonly ChatGroupMemberService _chatGroupMemeberService;
        private readonly AgentsTemplateService _agentsTemplateService;
        private readonly AIModelService _aIModelService;
        private readonly ChatTaskService _chatTaskService;
        private readonly PromptItemService _promptItemService;

        public ChatGroupAppService(IServiceProvider serviceProvider,
            ChatGroupService chatGroupService,
            ChatGroupMemberService chatGroupMemeberService,
            AgentsTemplateService agentsTemplateService,
            AIModelService aIModelService,
            ChatTaskService chatTaskService,
            PromptItemService promptItemService) : base(serviceProvider)
        {
            this._chatGroupService = chatGroupService;
            this._chatGroupMemeberService = chatGroupMemeberService;
            this._agentsTemplateService = agentsTemplateService;
            this._aIModelService = aIModelService;
            this._chatTaskService = chatTaskService;
            this._promptItemService = promptItemService;
        }

        [FunctionRender("管理 ChatGroup", "管理 ChatGroup", typeof(Register))]
        public async Task<StringAppResponse> ManageChatGroupManage(ChatGroup_ManageChatGroupRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                //群主
                if (request.Admin.SelectedValues.Count() == 0 || !int.TryParse(request.Admin.SelectedValues.First(), out int adminId))
                {
                    return "必须选择一位群主，请到 AgentTemplate 中设置！";
                }

                //对接人
                if (request.EnterAgent.SelectedValues.Count() == 0 || !int.TryParse(request.EnterAgent.SelectedValues.First(), out int enterAgentId))
                {
                    return "必须选择一位对接人，请到 AgentTemplate 中设置！";
                }

                //var agentsTemplateAdmin = await _agentsTemplateService.GetAgentTemplateAsync(adminId);
                //var agentsTemplateEnterAgent = await _agentsTemplateService.GetAgentTemplateAsync(enterAgent);

                //TODO:封装到 Service 中
                ChatGroup chatGroup = null;
                var chatGroupDto = new ChatGroupDto(request.Name, true, ChatGroupState.Unstart, request.Description, adminId, enterAgentId);
                var isNew = false;
                if (request.ChatGroup.IsSelected("New"))
                {
                    //新建
                    chatGroup = new ChatGroup(chatGroupDto);
                    isNew = false;
                }
                else
                {
                    int.TryParse(request.ChatGroup.SelectedValues.First(), out int chatGroupId);
                    chatGroup = await _chatGroupService.GetObjectAsync(z => z.Id == chatGroupId);
                    _chatGroupService.Mapper.Map<ChatGroup>(chatGroupDto);
                }

                await _chatGroupService.SaveObjectAsync(chatGroup);

                logger.Append($"ChatGroup {(isNew ? "新增" : "编辑")} 成功！");

                //添加成员
                var memberList = new List<ChatGroupMember>();
                var memberIdList = request.Members.SelectedValues.Select(z => int.Parse(z)).ToList();
                //合并“对接人”为成员
                if (!memberIdList.Contains(chatGroupDto.EnterAgentTemplateId))
                {
                    memberIdList.Add(chatGroupDto.EnterAgentTemplateId);
                }

                foreach (var agentId in memberIdList)
                {
                    var chatGroupMemberDto = new ChatGroupMemberDto(null, chatGroup.Id, agentId);
                    var member = new ChatGroupMember(chatGroupMemberDto);
                    member.ResetUID();
                    memberList.Add(member);
                }
                await _chatGroupMemeberService.SaveObjectListAsync(memberList);

                logger.Append($"ChatGroup 成员添加成功！");

                return logger.ToString();
            });
        }

        [FunctionRender("启动 ChatGroup", "启动 ChatGroup", typeof(Register))]
        public async Task<StringAppResponse> RunChatGroup(ChatGroup_RunChatGroupRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                //群主
                if (request.ChatGroups.SelectedValues.Count() == 0)
                {
                    return "至少选择一个组！";
                }

                var aiModelSelected = request.AIModel.SelectedValues.FirstOrDefault();
                var aiSetting = Senparc.AI.Config.SenparcAiSetting;
                if (aiModelSelected != "Default")
                {
                    int.TryParse(aiModelSelected, out int aiModelId);
                    var aiModel = await _aIModelService.GetObjectAsync(z => z.Id == aiModelId);
                    if (aiModel == null)
                    {
                        throw new NcfExceptionBase($"当前选择的 AI 模型不存在：{aiModelSelected}");
                    }

                    var aiModelDto = _aIModelService.Mapper.Map<AIModelDto>(aiModel);

                    aiSetting = _aIModelService.BuildSenparcAiSetting(aiModelDto);
                }

                List<Task> tasks = new List<Task>();

                foreach (var chatGroupId in request.ChatGroups.SelectedValues.Select(z => int.Parse(z)))
                {
                    var task =  _chatGroupService.RunChatGroup(logger, chatGroupId, request.Command, aiSetting, request.Individuation.IsSelected("1"));
                    tasks.Add(task);
                } 

                Task.WaitAll(tasks.ToArray());

                return logger.ToString();
            });
        }

        /// <summary>
        /// 创建或设置 ChatGroup
        /// </summary>
        /// <param name="chatGroupDto">ChatGroup 信息></param>
        /// <param name="memberAgentTemplateIds">成员 AgentTemplate ID</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<ChatGroup_SetGroupChatResponse>> SetChatGroup(ChatGroupDto chatGroupDto, List<int> memberAgentTemplateIds)
        {
            return await this.GetResponseAsync<ChatGroup_SetGroupChatResponse>(async (response, logger) =>
            {
                //var agentsTemplateAdmin = await _agentsTemplateService.GetAgentTemplateAsync(adminId);
                //var agentsTemplateEnterAgent = await _agentsTemplateService.GetAgentTemplateAsync(enterAgent);

                //TODO:封装到 Service 中
                ChatGroup chatGroup = null;
                chatGroupDto.State = ChatGroupState.Unstart;

                var isNew = false;
                var memberList = new List<ChatGroupMember>();
                if (chatGroupDto.Id == 0)
                {
                    //新建
                    chatGroup = new ChatGroup(chatGroupDto);
                    isNew = false;
                }
                else
                {
                    chatGroup = await _chatGroupService.GetObjectAsync(z => z.Id == chatGroupDto.Id);
                    chatGroup.Update(chatGroupDto);

                    memberList = await _chatGroupMemeberService.GetFullListAsync(z => z.ChatGroupId == chatGroupDto.Id);


                    //chatGroup = _chatGroupService.Mapper.Map<ChatGroup>(chatGroupDto);
                }

                await _chatGroupService.SaveObjectAsync(chatGroup);

                logger.Append($"ChatGroup {(isNew ? "新增" : "编辑")} 成功！");

                //添加成员

                //合并“对接人”为成员
                if (!memberAgentTemplateIds.Contains(chatGroupDto.EnterAgentTemplateId))
                {
                    memberAgentTemplateIds.Add(chatGroupDto.EnterAgentTemplateId);
                }

                foreach (var agentId in memberAgentTemplateIds)
                {
                    if (memberList.Exists(z => z.AgentTemplateId == agentId))
                    {
                        continue;//已存在的不添加
                    }

                    var chatGroupMemberDto = new ChatGroupMemberDto(null, chatGroup.Id, agentId);
                    var member = new ChatGroupMember(chatGroupMemberDto);
                    member.ResetUID();
                    memberList.Add(member);
                }

                //删除不在范围内的成员
                var tobeRemove = memberList.Where(z => !memberAgentTemplateIds.Contains(z.AgentTemplateId)).ToArray();

                for (var i = 0; i < tobeRemove.Length; i++) {
                    var member = tobeRemove[i];
                    memberList.Remove(member);
                    await _chatGroupMemeberService.DeleteObjectAsync(member);
                }

                await _chatGroupMemeberService.SaveObjectListAsync(memberList);

                logger.Append($"ChatGroup 成员添加成功！");

                return new ChatGroup_SetGroupChatResponse()
                {
                    Logs = logger.ToString(),
                    ChatGroupDto = this._chatGroupService.Mapping<ChatGroupDto>(chatGroup)
                };
            });
        }

        /// <summary>
        /// 创建或设置 ChatGroup
        /// </summary>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<ChatGroup_GetListResponse>> GetChatGroupList(int agentTemplateId, int pageIndex, int pageSize, string filter = "")
        {
            return await this.GetResponseAsync<ChatGroup_GetListResponse>(async (response, logger) =>
            {
                var chatGroupIdList = new List<int>();

                if (agentTemplateId > 0)
                {
                    var agentTemplateService = base.GetRequiredService<AgentTemplateAppService>();
                    var memberService = base.GetRequiredService<ChatGroupMemberService>();
                    var chatGroupList = await memberService.GetFullListAsync(z => z.AgentTemplateId == agentTemplateId);
                    chatGroupIdList = chatGroupList.Select(z => z.ChatGroupId).ToList();
                }

                var seh = new SenparcExpressionHelper<ChatGroup>();
                seh.ValueCompare
                    .AndAlso(agentTemplateId > 0, z => chatGroupIdList.Contains(z.Id));
                //增加模糊搜索组
                seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(filter), _ => _.Name.Contains(filter));
                var where = seh.BuildWhereExpression();

                var list = await this._chatGroupService.GetObjectListAsync(pageIndex, pageSize, where, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);

                return new ChatGroup_GetListResponse()
                {
                    ChatGroupDtoList = this._chatGroupService.Mapping<ChatGroupDto>(list)
                };
            });
        }

        /// <summary>
        /// 创建或设置 ChatGroup
        /// </summary>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<ChatGroup_GetItemResponse>> GetChatGroupItem(int id)
        {
            return await this.GetResponseAsync<ChatGroup_GetItemResponse>(async (response, logger) =>
            {
                var item = await this._chatGroupService.GetObjectAsync(z => z.Id == id);

                var agentTemplateService = base.GetRequiredService<AgentsTemplateService>();

                var chartGroupMemeberService = base.GetRequiredService<ChatGroupMemberService>();

                var members = await chartGroupMemeberService.GetFullListAsync(z => z.ChatGroupId == id, z => z.Id, Ncf.Core.Enums.OrderingType.Descending, new[] { nameof(ChatGroupMember.AgentTemplate) });


                var agents = members.Select(z => agentTemplateService.Mapping<AgentTemplateDto>(z.AgentTemplate)).ToList();

                return new ChatGroup_GetItemResponse()
                {
                    ChatGroupDto = this._chatGroupService.Mapping<ChatGroupDto>(item),
                    AgentTemplateDtoList = agents
                };
            });
        }

        /// <summary>
        /// 运行智能体
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> RunGroup(ChatGroup_RunGroupRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                List<Task> tasks = new List<Task>();

                //TODO: 使用线程进行维护
                var task = _chatGroupService.RunChatGroupInThread(request);
                tasks.Add(task);

                Task.WaitAll(tasks.ToArray());

                return logger.ToString();

            });
        }

        /// <summary>
        /// 获取 Agents 3D 视图所需的聚合快照数据
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<AgentGraphSnapshotResponse>> GetAgentGraphSnapshot(string filter = "")
        {
            return await this.GetResponseAsync<AgentGraphSnapshotResponse>(async (response, logger) =>
            {
                var agentExpression = new SenparcExpressionHelper<AgentTemplate>();
                agentExpression.ValueCompare.AndAlso(!string.IsNullOrWhiteSpace(filter), z => z.Name.Contains(filter));
                var agents = await _agentsTemplateService.GetObjectListAsync(0, 0, agentExpression.BuildWhereExpression(), z => z.Id, Ncf.Core.Enums.OrderingType.Descending);

                var result = new AgentGraphSnapshotResponse();
                if (agents.Count == 0)
                {
                    return result;
                }

                var agentIds = agents.Select(z => z.Id).Distinct().ToList();
                var members = await _chatGroupMemeberService.GetFullListAsync(z => agentIds.Contains(z.AgentTemplateId));

                // Include groups even when member rows are missing but admin/enter agent is configured.
                var groupsByRole = await _chatGroupService.GetFullListAsync(
                    z => agentIds.Contains(z.AdminAgentTemplateId) || agentIds.Contains(z.EnterAgentTemplateId),
                    z => z.Id,
                    Ncf.Core.Enums.OrderingType.Ascending);

                var groupIds = members
                    .Select(z => z.ChatGroupId)
                    .Concat(groupsByRole.Select(z => z.Id))
                    .Distinct()
                    .ToList();

                var groups = groupIds.Count > 0
                    ? await _chatGroupService.GetFullListAsync(z => groupIds.Contains(z.Id), z => z.Id, Ncf.Core.Enums.OrderingType.Ascending)
                    : new List<ChatGroup>();

                var tasks = groupIds.Count > 0
                    ? await _chatTaskService.GetFullListAsync(z => groupIds.Contains(z.ChatGroupId), z => z.Id, Ncf.Core.Enums.OrderingType.Descending)
                    : new List<ChatTask>();

                var activeTasks = tasks.Where(z =>
                    z.Status == ChatTask_Status.Waiting
                    || z.Status == ChatTask_Status.Chatting
                    || z.Status == ChatTask_Status.Paused).ToList();

                var activeTaskCountByGroup = activeTasks
                    .GroupBy(z => z.ChatGroupId)
                    .ToDictionary(g => g.Key, g => g.Count());

                var promptScoreCache = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

                foreach (var agent in agents)
                {
                    var memberGroupIds = members.Where(z => z.AgentTemplateId == agent.Id).Select(z => z.ChatGroupId).Distinct().ToList();
                    var chattingCount = memberGroupIds.Sum(groupId =>
                        activeTaskCountByGroup.TryGetValue(groupId, out var count) ? count : 0);

                    var score = await GetPromptScoreAsync(agent.PromptCode, promptScoreCache);

                    result.Agents.Add(new AgentGraphAgentDto
                    {
                        Id = agent.Id,
                        Name = agent.Name,
                        PromptCode = agent.PromptCode,
                        Score = score,
                        ChattingCount = chattingCount,
                        Enable = agent.Enable,
                        Avastar = agent.Avastar
                    });
                }

                foreach (var group in groups)
                {
                    var statusMap = tasks
                        .Where(z => z.ChatGroupId == group.Id)
                        .GroupBy(z => (int)z.Status)
                        .ToDictionary(g => g.Key, g => g.Count());

                    var memberAgentIds = members
                        .Where(z => z.ChatGroupId == group.Id)
                        .Select(z => z.AgentTemplateId)
                        .Distinct()
                        .ToList();

                    if (!memberAgentIds.Contains(group.AdminAgentTemplateId) && agentIds.Contains(group.AdminAgentTemplateId))
                    {
                        memberAgentIds.Add(group.AdminAgentTemplateId);
                    }

                    if (!memberAgentIds.Contains(group.EnterAgentTemplateId) && agentIds.Contains(group.EnterAgentTemplateId))
                    {
                        memberAgentIds.Add(group.EnterAgentTemplateId);
                    }

                    result.Groups.Add(new AgentGraphGroupDto
                    {
                        Id = group.Id,
                        Name = group.Name,
                        Enable = group.Enable,
                        State = (int)group.State,
                        RunningTaskCount = activeTaskCountByGroup.TryGetValue(group.Id, out var runningCount) ? runningCount : 0,
                        TaskStatusCounts = statusMap,
                        MemberAgentIds = memberAgentIds
                    });

                    foreach (var memberAgentId in memberAgentIds)
                    {
                        result.Links.Add(new AgentGraphLinkDto
                        {
                            GroupId = group.Id,
                            AgentId = memberAgentId
                        });
                    }
                }

                foreach (var task in activeTasks)
                {
                    var memberAgentIds = members
                        .Where(z => z.ChatGroupId == task.ChatGroupId)
                        .Select(z => z.AgentTemplateId)
                        .Distinct()
                        .ToList();

                    if (memberAgentIds.Count < 2)
                    {
                        continue;
                    }

                    result.Collaborations.Add(new AgentGraphCollaborationDto
                    {
                        TaskId = task.Id,
                        GroupId = task.ChatGroupId,
                        TaskName = task.Name,
                        Status = (int)task.Status,
                        AgentIds = memberAgentIds
                    });
                }

                return result;
            });
        }

        private async Task<float> GetPromptScoreAsync(string promptCode, Dictionary<string, float> scoreCache)
        {
            if (string.IsNullOrWhiteSpace(promptCode))
            {
                return -1;
            }

            if (scoreCache.TryGetValue(promptCode, out var cachedScore))
            {
                return cachedScore;
            }

            try
            {
                var promptItem = await _promptItemService.GetBestPromptAsync(promptCode, true);
                var score = promptItem == null ? -1 : (float)promptItem.EvalAvgScore;
                scoreCache[promptCode] = score;
                return score;
            }
            catch
            {
                scoreCache[promptCode] = -1;
                return -1;
            }
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> Delete(int id)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var result = await DeleteChatGroupByIdsAsync(new List<int> { id }, logger);
                return result;
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> DeleteBatch([FromBody] List<int> ids)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var result = await DeleteChatGroupByIdsAsync(ids, logger);
                return result;
            });
        }

        /// <summary>
        /// 删除整个对话（包括所有消息、任务等）
        /// </summary>
        [FunctionRender("删除对话", "删除整个对话及其所有数据", typeof(Register))]
        public async Task<StringAppResponse> DeleteChatGroup(ChatGroup_DeleteChatGroupRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                if (request.ChatGroups.SelectedValues.Count() == 0)
                {
                    return "请选择要删除的对话！";
                }

                // 检查是否确认删除
                if (!request.ConfirmDelete)
                {
                    return "请勾选\"确认删除\"复选框来确认删除操作！";
                }

                var chatGroupIdList = request.ChatGroups.SelectedValues
                    .Where(z => int.TryParse(z, out _))
                    .Select(z => int.Parse(z))
                    .ToList();

                return await DeleteChatGroupByIdsAsync(chatGroupIdList, logger);
            });
        }

        private async Task<string> DeleteChatGroupByIdsAsync(List<int> chatGroupIdList, AppServiceLogger logger)
        {
            if (chatGroupIdList == null || chatGroupIdList.Count == 0)
            {
                return "未提供组 ID";
            }

            chatGroupIdList = chatGroupIdList.Distinct().ToList();

            int deletedCount = 0;
            var errors = new List<string>();

            foreach (var chatGroupId in chatGroupIdList)
            {
                try
                {
                    // 获取对话信息
                    var chatGroup = await _chatGroupService.GetObjectAsync(z => z.Id == chatGroupId);
                    if (chatGroup == null)
                    {
                        errors.Add($"对话 ID {chatGroupId} 不存在");
                        continue;
                    }

                    // 1. 删除所有关联的消息和任务
                    var chatTaskService = base.GetRequiredService<ChatTaskService>();
                    var chatGroupHistoryService = base.GetRequiredService<ChatGroupHistoryService>();

                    // 先获取所有相关的 ChatTask
                    var chatTasks = await chatTaskService.GetFullListAsync(z => z.ChatGroupId == chatGroupId);
                    var chatTaskIds = chatTasks.Select(z => z.Id).ToList();

                    // 删除这些 ChatTask 下的所有 ChatGroupHistory
                    if (chatTaskIds.Count > 0)
                    {
                        var histories = await chatGroupHistoryService.GetFullListAsync(
                            z => chatTaskIds.Contains(z.ChatTaskId));
                        if (histories.Count > 0)
                        {
                            foreach (var history in histories)
                            {
                                await chatGroupHistoryService.DeleteObjectAsync(history);
                            }
                            logger.Append($"  ✓ 已删除 {histories.Count} 条消息记录");
                        }
                    }

                    // 2. 删除所有 ChatTask
                    if (chatTasks.Count > 0)
                    {
                        foreach (var chatTask in chatTasks)
                        {
                            await chatTaskService.DeleteObjectAsync(chatTask);
                        }
                        logger.Append($"  ✓ 已删除 {chatTasks.Count} 个对话任务");
                    }

                    // 3. 删除所有对话成员 (ChatGroupMember)
                    var members = await _chatGroupMemeberService.GetFullListAsync(z => z.ChatGroupId == chatGroupId);
                    if (members.Count > 0)
                    {
                        foreach (var member in members)
                        {
                            await _chatGroupMemeberService.DeleteObjectAsync(member);
                        }
                        logger.Append($"  ✓ 已删除 {members.Count} 个对话成员");
                    }

                    // 4. 最后删除对话本身 (ChatGroup)
                    await _chatGroupService.DeleteObjectAsync(chatGroup);
                    logger.Append($"✓ 对话 '{chatGroup.Name}' 及其所有数据已删除");

                    deletedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"删除对话 ID {chatGroupId} 失败: {ex.Message}");
                    logger.Append($"✗ 删除失败: {ex.Message}");
                }
            }

            // 生成删除摘要
            logger.Append($"\n========== 删除摘要 ==========");
            logger.Append($"✓ 成功删除: {deletedCount} 个对话");
            if (errors.Count > 0)
            {
                logger.Append($"✗ 失败: {errors.Count} 个对话");
                foreach (var error in errors)
                {
                    logger.Append($"  • {error}");
                }
            }
            logger.Append($"==============================");

            return logger.ToString();
        }
    }

    public class AgentGraphSnapshotResponse
    {
        public List<AgentGraphAgentDto> Agents { get; set; } = new List<AgentGraphAgentDto>();
        public List<AgentGraphGroupDto> Groups { get; set; } = new List<AgentGraphGroupDto>();
        public List<AgentGraphLinkDto> Links { get; set; } = new List<AgentGraphLinkDto>();
        public List<AgentGraphCollaborationDto> Collaborations { get; set; } = new List<AgentGraphCollaborationDto>();
    }

    public class AgentGraphAgentDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PromptCode { get; set; }
        public float Score { get; set; }
        public int ChattingCount { get; set; }
        public bool Enable { get; set; }
        public string Avastar { get; set; }
    }

    public class AgentGraphGroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Enable { get; set; }
        public int State { get; set; }
        public int RunningTaskCount { get; set; }
        public Dictionary<int, int> TaskStatusCounts { get; set; } = new Dictionary<int, int>();
        public List<int> MemberAgentIds { get; set; } = new List<int>();
    }

    public class AgentGraphLinkDto
    {
        public int GroupId { get; set; }
        public int AgentId { get; set; }
    }

    public class AgentGraphCollaborationDto
    {
        public int TaskId { get; set; }
        public int GroupId { get; set; }
        public string TaskName { get; set; }
        public int Status { get; set; }
        public List<int> AgentIds { get; set; } = new List<int>();
    }
}
