using Microsoft.CodeAnalysis.CSharp;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
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

        public ChatGroupAppService(IServiceProvider serviceProvider,
            ChatGroupService chatGroupService,
            ChatGroupMemberService chatGroupMemeberService,
            AgentsTemplateService agentsTemplateService,
            AIModelService aIModelService) : base(serviceProvider)
        {
            this._chatGroupService = chatGroupService;
            this._chatGroupMemeberService = chatGroupMemeberService;
            this._agentsTemplateService = agentsTemplateService;
            this._aIModelService = aIModelService;
        }

        [FunctionRender("管理 ChatGroup", "管理 ChatGroup", typeof(Register))]
        public async Task<StringAppResponse> ManageChatGroupManage(ChatGroup_ManageChatGroupRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                //Group owner
                if (request.Admin.SelectedValues.Count() == 0 || !int.TryParse(request.Admin.SelectedValues.First(), out int adminId))
                {
                    return "必须选择一位群主，请到 AgentTemplate 中设置！";
                }

                //docking person
                if (request.EnterAgent.SelectedValues.Count() == 0 || !int.TryParse(request.EnterAgent.SelectedValues.First(), out int enterAgentId))
                {
                    return "必须选择一位对接人，请到 AgentTemplate 中设置！";
                }

                //var agentsTemplateAdmin = await _agentsTemplateService.GetAgentTemplateAsync(adminId);
                //var agentsTemplateEnterAgent = await _agentsTemplateService.GetAgentTemplateAsync(enterAgent);

                //TODO: Encapsulated into Service
                ChatGroup chatGroup = null;
                var chatGroupDto = new ChatGroupDto(request.Name, true, ChatGroupState.Unstart, request.Description, adminId, enterAgentId);
                var isNew = false;
                if (request.ChatGroup.IsSelected("New"))
                {
                    //New
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

                //Add member
                var memberList = new List<ChatGroupMember>();
                var memberIdList = request.Members.SelectedValues.Select(z => int.Parse(z)).ToList();
                //Merge "connectors" as members
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
                //Group owner
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
        /// Create or set up a ChatGroup
        /// </summary>
        /// <param name="chatGroupDto">ChatGroup information></param>
        /// <param name="memberAgentTemplateIds">Member AgentTemplate ID</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<ChatGroup_SetGroupChatResponse>> SetChatGroup(ChatGroupDto chatGroupDto, List<int> memberAgentTemplateIds)
        {
            return await this.GetResponseAsync<ChatGroup_SetGroupChatResponse>(async (response, logger) =>
            {
                //var agentsTemplateAdmin = await _agentsTemplateService.GetAgentTemplateAsync(adminId);
                //var agentsTemplateEnterAgent = await _agentsTemplateService.GetAgentTemplateAsync(enterAgent);

                //TODO: Encapsulated into Service
                ChatGroup chatGroup = null;
                chatGroupDto.State = ChatGroupState.Unstart;

                var isNew = false;
                var memberList = new List<ChatGroupMember>();
                if (chatGroupDto.Id == 0)
                {
                    //New
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

                //Add member

                //Merge "connectors" as members
                if (!memberAgentTemplateIds.Contains(chatGroupDto.EnterAgentTemplateId))
                {
                    memberAgentTemplateIds.Add(chatGroupDto.EnterAgentTemplateId);
                }

                foreach (var agentId in memberAgentTemplateIds)
                {
                    if (memberList.Exists(z => z.AgentTemplateId == agentId))
                    {
                        continue;//Do not add existing ones
                    }

                    var chatGroupMemberDto = new ChatGroupMemberDto(null, chatGroup.Id, agentId);
                    var member = new ChatGroupMember(chatGroupMemberDto);
                    member.ResetUID();
                    memberList.Add(member);
                }

                //Remove members that are not in scope
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
        /// Create or set up a ChatGroup
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
                //Add fuzzy search group
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
        /// Create or set up a ChatGroup
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
        ///run agent
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> RunGroup(ChatGroup_RunGroupRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                List<Task> tasks = new List<Task>();

                //TODO: Use threads for maintenance
                var task = _chatGroupService.RunChatGroupInThread(request);
                tasks.Add(task);

                Task.WaitAll(tasks.ToArray());

                return logger.ToString();

            });
        }

        /// <summary>
        /// Delete the entire conversation (including all messages, tasks, etc.)
        /// </summary>
        [FunctionRender("删除对话", "删除整个对话及其所有数据", typeof(Register))]
        public async Task<StringAppResponse> DeleteChatGroup(ChatGroup_DeleteChatGroupRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                // Check if the conversation is selected
                if (request.ChatGroups.SelectedValues.Count() == 0)
                {
                    return "请选择要删除的对话！";
                }

                // Check if deletion is confirmed
                if (!request.ConfirmDelete)
                {
                    return "请勾选\"确认删除\"复选框来确认删除操作！";
                }

                var chatGroupIdList = request.ChatGroups.SelectedValues
                    .Where(z => int.TryParse(z, out _))
                    .Select(z => int.Parse(z))
                    .ToList();

                int deletedCount = 0;
                var errors = new List<string>();

                foreach (var chatGroupId in chatGroupIdList)
                {
                    try
                    {
                        // Get conversation information
                        var chatGroup = await _chatGroupService.GetObjectAsync(z => z.Id == chatGroupId);
                        if (chatGroup == null)
                        {
                            errors.Add($"对话 ID {chatGroupId} 不存在");
                            continue;
                        }

                        // 1. Delete all associated messages and tasks
                        var chatTaskService = base.GetRequiredService<ChatTaskService>();
                        var chatGroupHistoryService = base.GetRequiredService<ChatGroupHistoryService>();

                        // First get all related ChatTasks
                        var chatTasks = await chatTaskService.GetFullListAsync(z => z.ChatGroupId == chatGroupId);
                        var chatTaskIds = chatTasks.Select(z => z.Id).ToList();

                        // Delete all ChatGroupHistory under these ChatTasks
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

                        // 2. Delete all ChatTasks
                        if (chatTasks.Count > 0)
                        {
                            foreach (var chatTask in chatTasks)
                            {
                                await chatTaskService.DeleteObjectAsync(chatTask);
                            }
                            logger.Append($"  ✓ 已删除 {chatTasks.Count} 个对话任务");
                        }

                        // 3. Delete all chat members (ChatGroupMember)
                        var members = await _chatGroupMemeberService.GetFullListAsync(z => z.ChatGroupId == chatGroupId);
                        if (members.Count > 0)
                        {
                            foreach (var member in members)
                            {
                                await _chatGroupMemeberService.DeleteObjectAsync(member);
                            }
                            logger.Append($"  ✓ 已删除 {members.Count} 个对话成员");
                        }

                        // 4. Finally delete the conversation itself (ChatGroup)
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

                // Generate deletion summary
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
            });
        }
    }
}
