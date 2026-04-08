using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Senparc.CO2NET;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
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

        /// <summary>
        /// 列出所有可用的 Agent Group，返回 JSON 摘要，让 AI 判断哪个最合适。
        /// </summary>
        [KernelFunction, Description("列出系统中所有可用的 Agent Group（智能体组），返回每个组的 ID、名称、描述及成员 Agent 概要，供 AI 判断哪个组最适合执行用户的任务需求")]
        public async Task<string> ListAvailableGroups(
            [Description("可选的关键字，用于过滤 Group 名称或描述（留空表示列出全部）")] string keyword = "")
        {
            var groups = await _chatGroupService.GetFullListAsync(
                z => string.IsNullOrEmpty(keyword) || z.Name.Contains(keyword) || z.Description.Contains(keyword),
                z => z.Id,
                Ncf.Core.Enums.OrderingType.Ascending);

            if (groups == null || groups.Count == 0)
            {
                return "系统中尚未配置任何 Agent Group。请先在 Agents 管理模块中创建 Group 和 Agent。";
            }

            var allMembers = await _chatGroupMemberService.GetFullListAsync(
                z => groups.Select(g => g.Id).Contains(z.ChatGroupId),
                includes: "AgentTemplate");

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
                        name = m.AgentTemplate?.Name,
                        description = m.AgentTemplate?.Description,
                        enabled = m.AgentTemplate?.Enable ?? false
                    }).ToList()
                });
            }

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// 创建并立即启动一个 Agent Task。调用此函数前必须已获得用户的明确确认。
        /// </summary>
        [KernelFunction, Description("在指定的 Agent Group 中创建并立即启动一个新的 Agent Task。请务必先通过 ListAvailableGroups 找到合适的组并向用户说明方案、获得用户确认后，再调用此函数")]
        public async Task<string> CreateAndRunAgentTask(
            [Description("目标 Agent Group 的 ID（通过 ListAvailableGroups 获取）")] int chatGroupId,
            [Description("任务标题，简明描述本次任务")] string taskName,
            [Description("任务详细描述，即发送给 Agent 的指令内容")] string taskDescription,
            [Description("AI 模型 ID（传 0 表示使用系统默认模型）")] int aiModelId = 0,
            [Description("是否设为定时循环任务")] bool isScheduled = false,
            [Description("定时类型：0=固定间隔分钟数, 1=每天, 2=每周（指定星期几1-7）, 3=每月（指定日期1-31）。仅 isScheduled=true 时有效")] int scheduleType = 0,
            [Description("定时间隔值（含义随 scheduleType 变化）。仅 isScheduled=true 时有效")] int? scheduleIntervalMinutes = null)
        {
            // 验证 Group 是否存在
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
                sb.AppendLine($"✅ 任务已成功创建并启动！");
                sb.AppendLine($"- **任务名称**：{taskName}");
                sb.AppendLine($"- **所在 Group**：{group.Name}（ID: {chatGroupId}）");
                sb.AppendLine($"- **任务描述**：{taskDescription}");
                if (isScheduled)
                {
                    var typeNames = new[] { "固定间隔", "每天", "每周", "每月" };
                    sb.AppendLine($"- **定时类型**：{typeNames[Math.Min(scheduleType, 3)]}，间隔值：{scheduleIntervalMinutes}");
                }
                sb.AppendLine($"\n您可以前往 [Agents 管理](../AgentsManager) 查看任务执行进度。");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"任务创建失败：{ex.Message}";
            }
        }

        /// <summary>
        /// 建议创建一套新的 Group + Agent 配置方案（不执行实际创建），返回配置建议供用户确认。
        /// </summary>
        [KernelFunction, Description("当系统中没有合适的 Agent Group 时，根据任务描述生成一套推荐的 Group + Agent 配置方案（JSON），返回建议让用户确认，不执行实际创建")]
        public string ProposeNewGroupConfig(
            [Description("任务需求描述")] string taskRequirement,
            [Description("期望的 Group 名称")] string groupName = "",
            [Description("期望的 Agent 角色描述列表（逗号分隔）")] string agentRoles = "")
        {
            var suggestedGroupName = string.IsNullOrWhiteSpace(groupName)
                ? $"AutoGroup_{DateTime.Now:MMdd}"
                : groupName;

            var roles = string.IsNullOrWhiteSpace(agentRoles)
                ? new[] { "任务执行者", "结果汇总者" }
                : agentRoles.Split(',').Select(r => r.Trim()).ToArray();

            var proposal = new
            {
                action = "create_group_and_agents",
                groupName = suggestedGroupName,
                groupDescription = $"由 Admin AI Chat 为任务需求 [{taskRequirement}] 自动规划的 Group",
                agents = roles.Select((role, idx) => new
                {
                    name = $"{suggestedGroupName}_Agent{idx + 1}",
                    description = role,
                    note = "请在 Agents 管理模块中为此 Agent 配置合适的 PromptCode（可参考 PromptCatalyzer 创建流程）"
                }).ToList(),
                nextStep = "请在 Agents 管理模块中按照上述方案手动创建 Group 和 Agent，或告知我详细的 Agent 职责，我可以进一步细化方案"
            };

            return JsonSerializer.Serialize(proposal, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
