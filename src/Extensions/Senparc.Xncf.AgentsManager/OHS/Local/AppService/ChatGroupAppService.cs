using Microsoft.CodeAnalysis.CSharp;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Utility;
using Senparc.Ncf.XncfBase;
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
using System.Text;
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

                SenparcAI_GetByVersionResponse promptResult;

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

                SenparcAI_GetByVersionResponse promptResult;

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
        /// 快速组建团队并立即执行任务
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [FunctionRender("快速组建团队并执行任务", "快速选择成员，组建临时团队，并立即执行指定任务", typeof(Register))]
        public async Task<StringAppResponse> QuickTeamAndRun(ChatGroup_QuickTeamRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                if (!request.Members.SelectedValues.Any())
                {
                    return "至少需要选择一名成员！";
                }

                if (!int.TryParse(request.Admin.SelectedValues.FirstOrDefault(), out int adminId))
                {
                    return "必须选择一位群主！";
                }

                if (!int.TryParse(request.EnterAgent.SelectedValues.FirstOrDefault(), out int enterAgentId))
                {
                    return "必须选择一位对接人！";
                }

                if (string.IsNullOrWhiteSpace(request.Command))
                {
                    return "任务描述不能为空！";
                }

                // 创建团队名称
                var teamName = string.IsNullOrWhiteSpace(request.TeamName)
                    ? $"快速团队-{DateTime.Now:yyyyMMddHHmmss}"
                    : request.TeamName;

                // 创建新的 ChatGroup
                var chatGroupDto = new ChatGroupDto(teamName, true, ChatGroupState.Unstart, "通过快速组建功能创建", adminId, enterAgentId);
                var chatGroup = new ChatGroup(chatGroupDto);
                await _chatGroupService.SaveObjectAsync(chatGroup);
                logger.Append($"团队 [{teamName}] 创建成功！");

                // 添加成员（合并对接人到成员列表）
                var memberIdList = request.Members.SelectedValues.Select(z => int.Parse(z)).ToList();
                if (!memberIdList.Contains(enterAgentId))
                {
                    memberIdList.Add(enterAgentId);
                }

                var memberList = new List<ChatGroupMember>();
                foreach (var agentId in memberIdList)
                {
                    var chatGroupMemberDto = new ChatGroupMemberDto(null, chatGroup.Id, agentId);
                    var member = new ChatGroupMember(chatGroupMemberDto);
                    member.ResetUID();
                    memberList.Add(member);
                }
                await _chatGroupMemeberService.SaveObjectListAsync(memberList);
                logger.Append($"已添加 {memberList.Count} 名成员！");

                // 确定 AI 模型设置
                var aiModelSelected = request.AIModel.SelectedValues.FirstOrDefault();
                var aiSetting = Senparc.AI.Config.SenparcAiSetting;
                if (!string.IsNullOrEmpty(aiModelSelected) && aiModelSelected != "Default")
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

                // 执行任务
                logger.Append($"开始执行任务：{request.Command}");
                var runTask = _chatGroupService.RunChatGroup(logger, chatGroup.Id, request.Command, aiSetting, false);
                Task.WaitAll(new[] { (Task)runTask });

                return logger.ToString();
            });
        }

        /// <summary>
        /// 获取可用的 XNCF 模块列表（用于 AI Chat 自动附加模块功能）
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [FunctionRender("获取可用 XNCF 模块", "列举系统中所有已注册的 XNCF 模块及其可用功能，可用于 AI Chat 自动附加模块功能", typeof(Register))]
        public async Task<StringAppResponse> GetAvailableXncfModules(ChatGroup_GetAvailableXncfModulesRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                var registers = XncfRegisterManager.RegisterList;
                if (!registers.Any())
                {
                    return "当前没有已注册的 XNCF 模块。";
                }

                var sb = new StringBuilder();
                sb.AppendLine($"共发现 {registers.Count} 个 XNCF 模块：");
                sb.AppendLine();

                foreach (var register in registers)
                {
                    sb.AppendLine($"【{register.Name}】");
                    sb.AppendLine($"  UID: {register.Uid}");
                    sb.AppendLine($"  版本: {register.Version}");
                    if (!string.IsNullOrEmpty(register.Description))
                    {
                        sb.AppendLine($"  描述: {register.Description}");
                    }

                    if (Senparc.Ncf.XncfBase.Register.FunctionRenderCollection.TryGetValue(register.GetType(), out var functionGroup)
                        && functionGroup != null && functionGroup.Count > 0)
                    {
                        sb.AppendLine($"  可用功能 ({functionGroup.Count} 个):");
                        foreach (var function in functionGroup)
                        {
                            sb.AppendLine($"    - {function.Value.FunctionRenderAttribute.Name}: {function.Value.FunctionRenderAttribute.Description}");
                        }
                    }
                    else
                    {
                        sb.AppendLine("  可用功能: 无");
                    }
                    sb.AppendLine();
                }

                return sb.ToString().Replace("\r\n", "<br />");
            });
        }
    }
}
