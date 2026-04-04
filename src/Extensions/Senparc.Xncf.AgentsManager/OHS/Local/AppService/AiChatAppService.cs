using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.ChatCompletion;
using Senparc.AI;
using Senparc.AI.Entities;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    /// <summary>
    /// AI 对话 AppService，用于通过自然语言对话启动 AgentsManager 任务
    /// </summary>
    public class AiChatAppService : AppServiceBase
    {
        private readonly ChatGroupService _chatGroupService;
        private readonly ChatGroupMemberService _chatGroupMemberService;
        private readonly AgentsTemplateService _agentsTemplateService;
        private readonly AIModelService _aiModelService;

        public AiChatAppService(
            IServiceProvider serviceProvider,
            ChatGroupService chatGroupService,
            ChatGroupMemberService chatGroupMemberService,
            AgentsTemplateService agentsTemplateService,
            AIModelService aiModelService) : base(serviceProvider)
        {
            _chatGroupService = chatGroupService;
            _chatGroupMemberService = chatGroupMemberService;
            _agentsTemplateService = agentsTemplateService;
            _aiModelService = aiModelService;
        }

        /// <summary>
        /// 发送 AI 对话消息，AI 将理解用户意图并自动匹配或建议创建 ChatGroup 来运行任务
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AiChat_SendMessageResponse>> SendMessage(AiChat_SendMessageRequest request)
        {
            return await this.GetResponseAsync<AiChat_SendMessageResponse>(async (response, logger) =>
            {
                if (string.IsNullOrWhiteSpace(request.UserMessage))
                {
                    throw new NcfExceptionBase("消息内容不能为空");
                }

                // 获取 AI 设置
                var aiSetting = Senparc.AI.Config.SenparcAiSetting;
                if (request.AiModelId > 0)
                {
                    var aiModel = await _aiModelService.GetObjectAsync(z => z.Id == request.AiModelId);
                    if (aiModel == null)
                    {
                        throw new NcfExceptionBase($"当前选择的 AI 模型不存在：{request.AiModelId}");
                    }
                    var aiModelDto = _aiModelService.Mapper.Map<AIModelDto>(aiModel);
                    aiSetting = _aiModelService.BuildSenparcAiSetting(aiModelDto);
                }

                // 获取所有可用的 AgentTemplate 列表
                var agentTemplates = await _agentsTemplateService.GetFullListAsync(z => z.Enable, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending);
                var agentTemplateList = agentTemplates.Select(z => new
                {
                    z.Id,
                    z.Name,
                    z.Description,
                    z.SystemMessage
                }).ToList();

                // 获取所有现有 ChatGroup 列表
                var chatGroups = await _chatGroupService.GetFullListAsync(z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending);
                var chatGroupList = new List<object>();
                foreach (var cg in chatGroups)
                {
                    var members = await _chatGroupMemberService.GetFullListAsync(z => z.ChatGroupId == cg.Id);
                    var memberIds = members.Select(z => z.AgentTemplateId).ToList();
                    var memberNames = agentTemplates.Where(a => memberIds.Contains(a.Id)).Select(a => a.Name).ToList();
                    chatGroupList.Add(new
                    {
                        cg.Id,
                        cg.Name,
                        cg.Description,
                        AdminAgentTemplateId = cg.AdminAgentTemplateId,
                        EnterAgentTemplateId = cg.EnterAgentTemplateId,
                        Members = string.Join("、", memberNames)
                    });
                }

                // 构建系统 Prompt
                var systemPrompt = BuildSystemPrompt(agentTemplateList, chatGroupList);

                // 构建消息历史
                var promptConfigParameter = new PromptConfigParameter()
                {
                    MaxTokens = 2000,
                    Temperature = 0.3,
                    TopP = 0.5,
                };

                var semanticAiHandler = new SemanticAiHandler((SenparcAiSetting)aiSetting);
                var iWantToRun = semanticAiHandler.ChatConfig(
                    promptConfigParameter,
                    userId: $"AiChat_{Guid.NewGuid():N}",
                    maxHistoryStore: 20,
                    chatSystemMessage: systemPrompt,
                    senparcAiSetting: (SenparcAiSetting)aiSetting);

                // 将历史对话注入
                var hisgoryArgName = "history";
                var chatHistoryFromKernel = iWantToRun.StoredAiArguments.KernelArguments[hisgoryArgName] as ChatHistory;
                if (chatHistoryFromKernel != null && request.ChatHistory?.Count > 0)
                {
                    foreach (var msg in request.ChatHistory)
                    {
                        if (msg.Role == "user")
                            chatHistoryFromKernel.AddUserMessage(msg.Content);
                        else if (msg.Role == "assistant")
                            chatHistoryFromKernel.AddAssistantMessage(msg.Content);
                    }
                    iWantToRun.StoredAiArguments.KernelArguments[hisgoryArgName] = chatHistoryFromKernel;
                }

                // 调用 AI
                var aiResult = await semanticAiHandler.ChatAsync(iWantToRun, request.UserMessage, historyArgName: hisgoryArgName);
                var aiResponseText = aiResult.OutputString;

                // 解析 AI 返回的结构化结果
                var responseDto = ParseAiResponse(aiResponseText, agentTemplates, request.AiModelId);

                return responseDto;
            });
        }

        /// <summary>
        /// 用户确认后，自动创建 ChatGroup 并运行任务（通过 FunctionRender 对应方法调用）
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AiChat_SendMessageResponse>> ConfirmCreateAndRunGroup(AiChat_SuggestedGroupDto suggestedGroup)
        {
            return await this.GetResponseAsync<AiChat_SendMessageResponse>(async (response, logger) =>
            {
                if (suggestedGroup == null)
                {
                    throw new NcfExceptionBase("建议的 ChatGroup 信息不能为空");
                }

                // 通过 FunctionRender 机制调用 SetChatGroup 方法（复用已有逻辑）
                var chatGroupAppService = base.GetRequiredService<ChatGroupAppService>();

                var chatGroupDto = new ChatGroupDto(
                    suggestedGroup.Name,
                    true,
                    ChatGroupState.Unstart,
                    suggestedGroup.Description,
                    suggestedGroup.AdminAgentTemplateId,
                    suggestedGroup.EnterAgentTemplateId);

                // 调用 ChatGroupAppService 中的 SetChatGroup 方法（与 FunctionRender 方法对应）
                var setGroupResponse = await chatGroupAppService.SetChatGroup(chatGroupDto, suggestedGroup.MemberAgentTemplateIds ?? new List<int>());

                if (setGroupResponse.Success == false)
                {
                    throw new NcfExceptionBase($"创建 ChatGroup 失败：{setGroupResponse.ErrorMessage}");
                }

                var newGroupId = setGroupResponse.Data?.ChatGroupDto?.Id ?? 0;
                if (newGroupId == 0)
                {
                    throw new NcfExceptionBase("创建 ChatGroup 成功但未获取到 ID");
                }

                // 获取 AI 设置
                var aiSetting = Senparc.AI.Config.SenparcAiSetting;
                if (suggestedGroup.AiModelId > 0)
                {
                    var aiModel = await _aiModelService.GetObjectAsync(z => z.Id == suggestedGroup.AiModelId);
                    if (aiModel != null)
                    {
                        var aiModelDto = _aiModelService.Mapper.Map<AIModelDto>(aiModel);
                        aiSetting = _aiModelService.BuildSenparcAiSetting(aiModelDto);
                    }
                }

                // 通过 FunctionRender 机制调用 RunGroup 方法（复用已有逻辑）
                var runRequest = new ChatGroup_RunGroupRequest
                {
                    Name = $"AI对话任务-{DateTime.Now:yyyyMMddHHmmss}",
                    ChatGroupId = newGroupId,
                    AiModelId = suggestedGroup.AiModelId,
                    PromptCommand = suggestedGroup.TaskCommand,
                    Description = $"由 AI 对话自动创建并启动的任务",
                    Personality = true
                };

                // 使用 EventBus 模式，在独立线程中运行任务（不阻塞）
                var _ = Task.Run(async () =>
                {
                    try
                    {
                        await _chatGroupService.RunChatGroupInThread(runRequest);
                    }
                    catch (Exception ex)
                    {
                        Senparc.CO2NET.Trace.SenparcTrace.BaseExceptionLog(ex);
                    }
                });

                var taskName = runRequest.Name;

                return new AiChat_SendMessageResponse
                {
                    AiMessage = $"✅ 已成功创建组「{suggestedGroup.Name}」并启动任务「{taskName}」！任务正在后台运行中，您可以切换到「任务」标签页查看进度。",
                    ResponseType = AiChatResponseType.TaskStarted,
                    StartedGroupId = newGroupId,
                    StartedTaskName = taskName
                };
            });
        }

        /// <summary>
        /// 直接运行已有 ChatGroup 的任务（不创建新组）
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AiChat_SendMessageResponse>> RunExistingGroup(AiChat_RunExistingGroupRequest request)
        {
            return await this.GetResponseAsync<AiChat_SendMessageResponse>(async (response, logger) =>
            {
                var chatGroup = await _chatGroupService.GetObjectAsync(z => z.Id == request.ChatGroupId);
                if (chatGroup == null)
                {
                    throw new NcfExceptionBase($"未找到 ChatGroup（ID：{request.ChatGroupId}）");
                }

                var runRequest = new ChatGroup_RunGroupRequest
                {
                    Name = $"AI对话任务-{DateTime.Now:yyyyMMddHHmmss}",
                    ChatGroupId = request.ChatGroupId,
                    AiModelId = request.AiModelId,
                    PromptCommand = request.TaskCommand,
                    Description = "由 AI 对话自动启动的任务",
                    Personality = true
                };

                // 使用 EventBus 模式，在独立线程中运行任务（不阻塞）
                var _ = Task.Run(async () =>
                {
                    try
                    {
                        await _chatGroupService.RunChatGroupInThread(runRequest);
                    }
                    catch (Exception ex)
                    {
                        Senparc.CO2NET.Trace.SenparcTrace.BaseExceptionLog(ex);
                    }
                });

                return new AiChat_SendMessageResponse
                {
                    AiMessage = $"✅ 已使用组「{chatGroup.Name}」启动任务「{runRequest.Name}」！任务正在后台运行中，您可以切换到「任务」标签页查看进度。",
                    ResponseType = AiChatResponseType.TaskStarted,
                    StartedGroupId = request.ChatGroupId,
                    StartedTaskName = runRequest.Name
                };
            });
        }

        #region 私有辅助方法

        /// <summary>
        /// 构建系统 Prompt
        /// </summary>
        private string BuildSystemPrompt(
            IEnumerable<object> agentTemplates,
            IEnumerable<object> chatGroups)
        {
            var agentJson = JsonSerializer.Serialize(agentTemplates, new JsonSerializerOptions { WriteIndented = false });
            var groupJson = JsonSerializer.Serialize(chatGroups, new JsonSerializerOptions { WriteIndented = false });

            var sb = new StringBuilder();
            sb.AppendLine("你是一个 AgentsManager 任务助手，帮助用户通过自然语言启动 AI 多智能体协作任务。");
            sb.AppendLine();
            sb.AppendLine("## 可用的智能体（AgentTemplate）列表：");
            sb.AppendLine(agentJson);
            sb.AppendLine();
            sb.AppendLine("## 现有的智能体组（ChatGroup）列表：");
            sb.AppendLine(groupJson);
            sb.AppendLine();
            sb.AppendLine("## 你的职责：");
            sb.AppendLine("1. 理解用户的任务需求");
            sb.AppendLine("2. 判断现有 ChatGroup 是否适合执行该任务：");
            sb.AppendLine("   - 如果有合适的组，返回 JSON 格式：{\"action\":\"run_existing\",\"groupId\":组ID,\"reason\":\"原因\",\"taskCommand\":\"任务命令\"}");
            sb.AppendLine("   - 如果没有合适的组，分析需要哪些智能体，返回 JSON 格式：{\"action\":\"create_group\",\"reason\":\"为什么需要创建新组\",\"suggestion\":{\"name\":\"建议的组名称\",\"description\":\"组说明\",\"adminAgentTemplateId\":群主ID,\"enterAgentTemplateId\":对接人ID,\"memberIds\":[成员ID列表],\"taskCommand\":\"任务命令\"}}");
            sb.AppendLine("3. 如果用户的输入不是任务需求（如闲聊、问候），正常回复，返回 JSON 格式：{\"action\":\"message\",\"content\":\"你的回复内容\"}");
            sb.AppendLine();
            sb.AppendLine("## 重要说明：");
            sb.AppendLine("- adminAgentTemplateId 通常选择名称包含\"主\"或者排在首位的智能体");
            sb.AppendLine("- enterAgentTemplateId 是负责接收用户命令的智能体（对接人）");
            sb.AppendLine("- memberIds 包含所有参与任务的智能体 ID");
            sb.AppendLine("- 请严格按照 JSON 格式返回，不要添加任何 Markdown 代码块标记或额外说明");
            sb.Append("- 只返回纯 JSON，不要有其他文字");
            return sb.ToString();
        }

        /// <summary>
        /// 解析 AI 返回结果
        /// </summary>
        private AiChat_SendMessageResponse ParseAiResponse(
            string aiResponseText,
            IEnumerable<AgentTemplate> agentTemplates,
            int aiModelId)
        {
            try
            {
                // 清理可能的 Markdown 代码块标记
                var cleanedText = aiResponseText
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                var jsonDoc = JsonDocument.Parse(cleanedText);
                var root = jsonDoc.RootElement;

                var action = root.GetProperty("action").GetString();

                switch (action)
                {
                    case "run_existing":
                        var groupId = root.GetProperty("groupId").GetInt32();
                        var reason = root.TryGetProperty("reason", out var reasonEl) ? reasonEl.GetString() : "";
                        var taskCmd = root.TryGetProperty("taskCommand", out var tcEl) ? tcEl.GetString() : "";
                        return new AiChat_SendMessageResponse
                        {
                            AiMessage = $"已找到合适的组来执行您的任务。{reason}\n\n任务命令：{taskCmd}",
                            ResponseType = AiChatResponseType.SuggestRunTask,
                            SuggestedGroupId = groupId,
                            SuggestedGroup = new AiChat_SuggestedGroupDto
                            {
                                TaskCommand = taskCmd,
                                AiModelId = aiModelId
                            }
                        };

                    case "create_group":
                        var createReason = root.TryGetProperty("reason", out var crEl) ? crEl.GetString() : "";
                        var suggestion = root.GetProperty("suggestion");

                        var suggestedName = suggestion.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : "新智能体组";
                        var suggestedDesc = suggestion.TryGetProperty("description", out var descEl) ? descEl.GetString() : "";
                        var adminId = suggestion.TryGetProperty("adminAgentTemplateId", out var adminEl) ? adminEl.GetInt32() : 0;
                        var enterId = suggestion.TryGetProperty("enterAgentTemplateId", out var enterEl) ? enterEl.GetInt32() : 0;
                        var memberIds = new List<int>();
                        if (suggestion.TryGetProperty("memberIds", out var memberIdsEl))
                        {
                            foreach (var idEl in memberIdsEl.EnumerateArray())
                            {
                                memberIds.Add(idEl.GetInt32());
                            }
                        }
                        var createTaskCmd = suggestion.TryGetProperty("taskCommand", out var ctcEl) ? ctcEl.GetString() : "";

                        // 获取智能体名称列表用于展示
                        var agentList = agentTemplates.ToList();
                        var memberNames = agentList.Where(a => memberIds.Contains(a.Id)).Select(a => a.Name).ToList();
                        var adminName = agentList.FirstOrDefault(a => a.Id == adminId)?.Name ?? $"ID:{adminId}";
                        var enterName = agentList.FirstOrDefault(a => a.Id == enterId)?.Name ?? $"ID:{enterId}";

                        var sb = new StringBuilder();
                        sb.AppendLine($"当前没有合适的组来执行您的任务。{createReason}");
                        sb.AppendLine();
                        sb.AppendLine($"**建议创建新组：**");
                        sb.AppendLine($"- **组名称**：{suggestedName}");
                        if (!string.IsNullOrEmpty(suggestedDesc))
                            sb.AppendLine($"- **说明**：{suggestedDesc}");
                        sb.AppendLine($"- **群主**：{adminName}");
                        sb.AppendLine($"- **对接人**：{enterName}");
                        if (memberNames.Count > 0)
                            sb.AppendLine($"- **成员**：{string.Join("、", memberNames)}");
                        sb.AppendLine($"- **任务命令**：{createTaskCmd}");
                        sb.AppendLine();
                        sb.AppendLine("是否确认创建该组并运行任务？");

                        return new AiChat_SendMessageResponse
                        {
                            AiMessage = sb.ToString(),
                            ResponseType = AiChatResponseType.SuggestCreateGroup,
                            SuggestedGroup = new AiChat_SuggestedGroupDto
                            {
                                Name = suggestedName,
                                Description = suggestedDesc,
                                AdminAgentTemplateId = adminId,
                                EnterAgentTemplateId = enterId,
                                MemberAgentTemplateIds = memberIds,
                                TaskCommand = createTaskCmd,
                                AiModelId = aiModelId
                            }
                        };

                    case "message":
                    default:
                        var content = root.TryGetProperty("content", out var contentEl)
                            ? contentEl.GetString()
                            : aiResponseText;
                        return new AiChat_SendMessageResponse
                        {
                            AiMessage = content,
                            ResponseType = AiChatResponseType.Message
                        };
                }
            }
            catch (Exception ex)
            {
                // 解析失败时，将原始文本作为普通消息返回（AI 可能返回非 JSON 格式的内容）
                Senparc.CO2NET.Trace.SenparcTrace.Log($"AiChatAppService ParseAiResponse 解析异常: {ex.Message}，原始响应: {aiResponseText?.Substring(0, Math.Min(200, aiResponseText?.Length ?? 0))}");
                return new AiChat_SendMessageResponse
                {
                    AiMessage = aiResponseText,
                    ResponseType = AiChatResponseType.Message
                };
            }
        }

        #endregion
    }
}
