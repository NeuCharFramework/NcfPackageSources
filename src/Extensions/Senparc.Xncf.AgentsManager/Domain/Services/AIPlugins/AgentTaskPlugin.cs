using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services.AIPlugins
{
    /// <summary>
    /// Admin AI Chat 中用于"Agents 任务"场景的 Kernel Function 插件。
    /// AI 可通过此插件搜索合适的 Agent Group，并在用户确认后自动创建并启动任务。
    /// <para>同时标注 [FunctionRender] 使 AdminChatAiService 能自动将其加载到 Semantic Kernel 中。</para>
    /// </summary>
    public class AgentTaskPlugin
    {
        private readonly ChatGroupService _chatGroupService;
        private readonly ChatGroupMemberService _chatGroupMemberService;
        private readonly AgentsTemplateService _agentsTemplateService;
        private readonly ChatTaskService _chatTaskService;

        public AgentTaskPlugin()
        {
            var sp = SenparcDI.GetServiceProvider();
            _chatGroupService = sp.GetRequiredService<ChatGroupService>();
            _chatGroupMemberService = sp.GetRequiredService<ChatGroupMemberService>();
            _agentsTemplateService = sp.GetRequiredService<AgentsTemplateService>();
            _chatTaskService = sp.GetRequiredService<ChatTaskService>();
        }

        // ──────────────────────────────────────────────────────────────────────
        // 1. 搜索可用 Group
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 列出所有可用的 Agent Group，返回 JSON 摘要，让 AI 判断哪个最合适。
        /// </summary>
        [KernelFunction, Description("列出系统中所有可用的 Agent Group（智能体组），返回每个组的 ID、名称、描述及成员 Agent 概要，供 AI 判断哪个组最适合执行用户的任务需求")]
        [FunctionRender("列出 Agent Group", "列出系统中所有可用的 Agent Group 供 AI Chat 选择", typeof(Register))]
        public async Task<string> ListAvailableGroups(
            [Description("可选的关键字，用于过滤 Group 名称或描述（留空表示列出全部）")] string keyword = "")
        {
            var groups = await _chatGroupService.GetFullListAsync(
                z => string.IsNullOrEmpty(keyword) || z.Name.Contains(keyword) || z.Description.Contains(keyword),
                z => z.Id,
                Ncf.Core.Enums.OrderingType.Ascending);

            if (groups == null || groups.Count == 0)
            {
                return "系统中尚未配置任何 Agent Group。请先调用 CreateGroupWithAgents 创建一个适合的 Group。";
            }

            var allMembers = await _chatGroupMemberService.GetFullListAsync(
                z => groups.Select(g => g.Id).Contains(z.ChatGroupId));

            var agentIds = allMembers.Select(m => m.AgentTemplateId).Distinct().ToList();
            var agents = await _agentsTemplateService.GetFullListAsync(z => agentIds.Contains(z.Id));
            var agentMap = agents.ToDictionary(a => a.Id);

            var result = new List<object>();
            foreach (var g in groups)
            {
                var members = allMembers.Where(m => m.ChatGroupId == g.Id).ToList();
                result.Add(new
                {
                    groupId = g.Id,
                    name = g.Name,
                    description = g.Description,
                    agentCount = members.Count,
                    agents = members.Select(m => new
                    {
                        id = m.AgentTemplateId,
                        name = agentMap.TryGetValue(m.AgentTemplateId, out var a) ? a.Name : "(unknown)",
                        description = a?.Description,
                        enabled = a?.Enable ?? false,
                        systemMessagePreview = a != null && !string.IsNullOrWhiteSpace(a.SystemMessage)
                            ? (a.SystemMessage.Length > 80 ? a.SystemMessage.Substring(0, 80) + "…" : a.SystemMessage)
                            : "(未配置)"
                    }).ToList()
                });
            }

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }

        // ──────────────────────────────────────────────────────────────────────
        // 2. 创建 Group + Agent（含 PromptCode）后自动启动任务
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 当系统中没有合适的 Group 时，根据用户确认的配置自动创建 Group、Agent（含 SystemMessage/PromptCode）并立即启动第一个任务。
        /// 请务必先向用户展示配置方案并获得明确确认，再调用此函数。
        /// </summary>
        [KernelFunction, Description("在用户确认后，自动创建 Agent Group、Agent（含 SystemMessage/PromptCode）和 ChatGroupMember，然后立即启动第一个任务。调用前须已获得用户明确确认。")]
        [FunctionRender("自动创建 Group+Agent 并启动任务", "为 AI Chat 自动创建 Agent Group、Agent 和 PromptCode，并立即启动任务", typeof(Register))]
        public async Task<string> CreateGroupWithAgents(
            [Description("新 Group 的名称")] string groupName,
            [Description("新 Group 的描述")] string groupDescription,
            [Description("Agent 的名称")] string agentName,
            [Description("Agent 的角色职责描述")] string agentRoleDescription,
            [Description("Agent 的完整 SystemMessage/PromptCode（由 AI 根据角色职责生成的详细系统提示词）")] string agentSystemPrompt,
            [Description("要立即执行的任务描述（用于启动第一个 ChatTask）")] string taskDescription,
            [Description("任务标题")] string taskName = "",
            [Description("AI 模型 ID（传 0 表示使用系统默认模型）")] int aiModelId = 0)
        {
            var sb = new StringBuilder();
            try
            {
                // 步骤1：创建 AgentTemplate
                var agentDescription = string.IsNullOrWhiteSpace(agentRoleDescription)
                    ? $"由 Admin AI Chat 自动创建 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                    : agentRoleDescription;

                var newAgent = new AgentTemplate(
                    name: agentName,
                    systemMessage: agentSystemPrompt,
                    enable: true,
                    description: agentDescription,
                    promptCode: agentSystemPrompt,   // 直接使用 AI 生成的 SystemPrompt 作为 PromptCode
                    hookRobotType: HookRobotType.None,
                    hookRobotParameter: null,
                    avastar: null,
                    functionCallNames: null,
                    mcpEndpoints: null);

                await _agentsTemplateService.SaveObjectAsync(newAgent);
                sb.AppendLine($"✅ Agent 创建成功：{agentName}（ID: {newAgent.Id}）");

                // 步骤2：创建 ChatGroup
                var newGroup = new ChatGroup(
                    name: groupName,
                    enable: true,
                    state: ChatGroupState.Running,
                    description: groupDescription,
                    adminAgentTemplateId: newAgent.Id,
                    enterAgentTemplateId: newAgent.Id);

                await _chatGroupService.SaveObjectAsync(newGroup);
                sb.AppendLine($"✅ Group 创建成功：{groupName}（ID: {newGroup.Id}）");

                // 步骤3：创建 ChatGroupMember
                var member = new ChatGroupMember(
                    agentTemplateId: newAgent.Id,
                    agentTemplate: newAgent,
                    chatGroupId: newGroup.Id);

                await _chatGroupMemberService.SaveObjectAsync(member);
                sb.AppendLine("✅ Group 成员配置完成");

                // 步骤4：启动第一个任务
                var finalTaskName = string.IsNullOrWhiteSpace(taskName)
                    ? $"{groupName} 首次任务 {DateTime.Now:MM-dd HH:mm}"
                    : taskName;

                var sp = SenparcDI.GetServiceProvider();
                var chatGroupService = sp.GetRequiredService<ChatGroupService>();

                var request = new ChatGroup_RunGroupRequest
                {
                    Name = finalTaskName,
                    ChatGroupId = newGroup.Id,
                    AiModelId = aiModelId,
                    PromptCommand = taskDescription,
                    Description = $"由 Admin AI Chat 自动创建并启动 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    Personality = true
                };

                await chatGroupService.RunChatGroupInThread(request);
                sb.AppendLine($"✅ 任务已启动：{finalTaskName}");
                sb.AppendLine();
                sb.AppendLine("**配置摘要**");
                sb.AppendLine($"- Group：{groupName}（ID: {newGroup.Id}）");
                sb.AppendLine($"- Agent：{agentName}（ID: {newAgent.Id}）");
                sb.AppendLine($"- 任务：{finalTaskName}");
                sb.AppendLine();
                sb.AppendLine("您可以前往 [Agents 管理](../AgentsManager) 查看任务执行进度。");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"❌ 创建失败：{ex.Message}");
            }

            return sb.ToString();
        }

        // ──────────────────────────────────────────────────────────────────────
        // 3. 在已有 Group 中创建并启动任务
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 创建并立即启动一个 Agent Task。调用此函数前必须已获得用户的明确确认。
        /// </summary>
        [KernelFunction, Description("在指定的 Agent Group 中创建并立即启动一个新的 Agent Task。请务必先通过 ListAvailableGroups 找到合适的组并向用户说明方案、获得用户确认后，再调用此函数")]
        [FunctionRender("创建并启动 Agent Task", "在已有 Group 中创建并启动任务（AI Chat 使用）", typeof(Register))]
        public async Task<string> CreateAndRunAgentTask(
            [Description("目标 Agent Group 的 ID（通过 ListAvailableGroups 获取）")] int chatGroupId,
            [Description("任务标题，简明描述本次任务")] string taskName,
            [Description("任务详细描述，即发送给 Agent 的指令内容")] string taskDescription,
            [Description("AI 模型 ID（传 0 表示使用系统默认模型）")] int aiModelId = 0,
            [Description("是否设为定时循环任务")] bool isScheduled = false,
            [Description("定时类型：0=固定间隔分钟数, 1=每天, 2=每周（指定星期几1-7）, 3=每月（指定日期1-31）。仅 isScheduled=true 时有效")] int scheduleType = 0,
            [Description("定时间隔值（含义随 scheduleType 变化）。仅 isScheduled=true 时有效")] int? scheduleIntervalMinutes = null)
        {
            var group = await _chatGroupService.GetObjectAsync(z => z.Id == chatGroupId);
            if (group == null)
            {
                return $"错误：找不到 ID={chatGroupId} 的 Agent Group，请先通过 ListAvailableGroups 确认正确的 Group ID。";
            }

            var request = new ChatGroup_RunGroupRequest
            {
                Name = taskName,
                ChatGroupId = chatGroupId,
                AiModelId = aiModelId,
                PromptCommand = taskDescription,
                Description = $"由 Admin AI Chat 自动创建 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                Personality = true,
                IsScheduled = isScheduled,
                ScheduleType = (ScheduleType)scheduleType,
                ScheduleIntervalMinutes = scheduleIntervalMinutes
            };

            try
            {
                var sp = SenparcDI.GetServiceProvider();
                var chatGroupService = sp.GetRequiredService<ChatGroupService>();
                await chatGroupService.RunChatGroupInThread(request);

                var sb = new StringBuilder();
                sb.AppendLine("✅ 任务已成功创建并启动！");
                sb.AppendLine($"- **任务名称**：{taskName}");
                sb.AppendLine($"- **所在 Group**：{group.Name}（ID: {chatGroupId}）");
                sb.AppendLine($"- **任务描述**：{taskDescription}");
                if (isScheduled)
                {
                    var typeNames = new[] { "固定间隔", "每天", "每周", "每月" };
                    sb.AppendLine($"- **定时类型**：{typeNames[Math.Min(scheduleType, 3)]}，间隔值：{scheduleIntervalMinutes}");
                }
                sb.AppendLine();
                sb.AppendLine("您可以前往 [Agents 管理](../AgentsManager) 查看任务执行进度。");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"任务创建失败：{ex.Message}";
            }
        }
    }
}

