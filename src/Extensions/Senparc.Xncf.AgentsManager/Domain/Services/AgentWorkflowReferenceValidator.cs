/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AgentWorkflowReferenceValidator.cs
    文件功能描述：AgentWorkflowReferenceValidator.cs 相关实现


    创建标识：Senparc - 20260822

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 增强 Agent 工作流校验、函数绑定与任务管理交互

----------------------------------------------------------------*/

using Senparc.Xncf.AgentsManager.Abstractions;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
/// Resolves Agent, Group and Workflow references as one directed graph.
/// The graph is read at validation time, so a multi-level cycle cannot hide behind
/// an intermediate Agent, Group or sub-workflow.
/// </summary>
public sealed class AgentWorkflowReferenceValidator : IAgentWorkflowReferenceValidator
{
    private readonly AgentsTemplateService _agentService;
    private readonly ChatGroupService _groupService;
    private readonly ChatGroupMemberService _memberService;
    private readonly IWorkflowDependencyProvider? _workflowProvider;

    public AgentWorkflowReferenceValidator(
        AgentsTemplateService agentService,
        ChatGroupService groupService,
        ChatGroupMemberService memberService,
        IWorkflowDependencyProvider? workflowProvider = null)
    {
        _agentService = agentService;
        _groupService = groupService;
        _memberService = memberService;
        _workflowProvider = workflowProvider;
    }

    public async Task<string?> ValidateWorkflowReferencesAsync(
        int workflowId,
        int adminUserId,
        IReadOnlyCollection<AgentWorkflowReference> references,
        CancellationToken cancellationToken = default)
    {
        if (workflowId <= 0 || references == null || references.Count == 0)
        {
            return null;
        }

        if (_workflowProvider == null)
        {
            return "NeuChar Workflow 模块未安装或未开启，无法验证 Workflow 循环引用。";
        }

        foreach (var reference in references.Where(item => item != null && item.Id > 0))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var reachesWorkflow = reference.Kind?.ToLowerInvariant() switch
            {
                "agent" => await AgentReachesWorkflowAsync(
                    reference.Id,
                    workflowId,
                    adminUserId,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    cancellationToken).ConfigureAwait(false),
                "group" => await GroupReachesWorkflowAsync(
                    reference.Id,
                    workflowId,
                    adminUserId,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    cancellationToken).ConfigureAwait(false),
                _ => false
            };

            if (reachesWorkflow)
            {
                return "工作流引用的 Agent/Group 通过 Function Calling 间接调用当前工作流，保存会形成循环引用。";
            }
        }

        return null;
    }

    public async Task<string?> ValidateAgentBindingsAsync(
        int agentId,
        int adminUserId,
        IReadOnlyCollection<int> workflowIds,
        CancellationToken cancellationToken = default)
    {
        if (agentId <= 0)
        {
            return null;
        }

        var workflowBindings = (workflowIds ?? Array.Empty<int>())
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (workflowBindings.Count == 0)
        {
            return null;
        }

        if (_workflowProvider == null)
        {
            return "NeuChar Workflow 模块未安装或未开启，无法保存 Workflow 绑定。";
        }

        if (adminUserId <= 0)
        {
            return "当前管理员身份不可用，无法验证 Workflow 循环引用。";
        }

        foreach (var workflowId in workflowBindings)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var reachesAgent = await WorkflowReachesAgentAsync(
                workflowId,
                agentId,
                adminUserId,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                cancellationToken).ConfigureAwait(false);
            if (reachesAgent)
            {
                return "该 Workflow 的多级 Agent/Group/子工作流引用会回到当前 Agent，已拒绝保存以避免循环调用。";
            }
        }

        return null;
    }

    private async Task<bool> AgentReachesWorkflowAsync(
        int agentId,
        int targetWorkflowId,
        int adminUserId,
        HashSet<string> path,
        CancellationToken cancellationToken)
    {
        var nodeKey = $"agent:{agentId}";
        if (!path.Add(nodeKey))
        {
            return false;
        }

        var agent = await _agentService.GetObjectAsync(item => item.Id == agentId).ConfigureAwait(false);
        if (agent == null)
        {
            return false;
        }

        foreach (var binding in AgentFunctionBindingCodec.Parse(agent.FunctionCallNames)
            .Where(AgentFunctionBindingCodec.IsWorkflowBinding))
        {
            var workflowId = ParseWorkflowId(binding);
            if (workflowId <= 0)
            {
                continue;
            }

            if (workflowId == targetWorkflowId)
            {
                return true;
            }

            if (await WorkflowReachesWorkflowOrAgentAsync(
                    workflowId,
                    targetWorkflowId,
                    null,
                    adminUserId,
                    new HashSet<string>(path, StringComparer.OrdinalIgnoreCase),
                    cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> WorkflowReachesAgentAsync(
        int workflowId,
        int targetAgentId,
        int adminUserId,
        HashSet<string> path,
        CancellationToken cancellationToken)
    {
        return await WorkflowReachesWorkflowOrAgentAsync(
            workflowId,
            null,
            targetAgentId,
            adminUserId,
            path,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> WorkflowReachesWorkflowOrAgentAsync(
        int workflowId,
        int? targetWorkflowId,
        int? targetAgentId,
        int adminUserId,
        HashSet<string> path,
        CancellationToken cancellationToken)
    {
        var nodeKey = $"workflow:{workflowId}";
        if (!path.Add(nodeKey))
        {
            return false;
        }

        var snapshot = await _workflowProvider.GetSnapshotAsync(
            workflowId,
            adminUserId,
            cancellationToken).ConfigureAwait(false);
        if (snapshot == null)
        {
            return false;
        }

        foreach (var reference in snapshot.References ?? Array.Empty<WorkflowDependencyReference>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(reference.Kind, "workflow", StringComparison.OrdinalIgnoreCase))
            {
                if (targetWorkflowId == reference.Id)
                {
                    return true;
                }

                if (await WorkflowReachesWorkflowOrAgentAsync(
                        reference.Id,
                        targetWorkflowId,
                        targetAgentId,
                        adminUserId,
                        new HashSet<string>(path, StringComparer.OrdinalIgnoreCase),
                        cancellationToken).ConfigureAwait(false))
                {
                    return true;
                }
            }
            else if (string.Equals(reference.Kind, "agent", StringComparison.OrdinalIgnoreCase))
            {
                if (targetAgentId == reference.Id)
                {
                    return true;
                }

                if (await AgentReachesWorkflowOrAgentAsync(
                        reference.Id,
                        targetWorkflowId,
                        targetAgentId,
                        adminUserId,
                        new HashSet<string>(path, StringComparer.OrdinalIgnoreCase),
                        cancellationToken).ConfigureAwait(false))
                {
                    return true;
                }
            }
            else if (string.Equals(reference.Kind, "group", StringComparison.OrdinalIgnoreCase)
                     && await GroupReachesWorkflowOrAgentAsync(
                         reference.Id,
                         targetWorkflowId,
                         targetAgentId,
                         adminUserId,
                         new HashSet<string>(path, StringComparer.OrdinalIgnoreCase),
                         cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> AgentReachesWorkflowOrAgentAsync(
        int agentId,
        int? targetWorkflowId,
        int? targetAgentId,
        int adminUserId,
        HashSet<string> path,
        CancellationToken cancellationToken)
    {
        var nodeKey = $"agent:{agentId}";
        if (!path.Add(nodeKey))
        {
            return false;
        }

        if (targetAgentId == agentId)
        {
            return true;
        }

        var agent = await _agentService.GetObjectAsync(item => item.Id == agentId).ConfigureAwait(false);
        if (agent == null)
        {
            return false;
        }

        foreach (var binding in AgentFunctionBindingCodec.Parse(agent.FunctionCallNames)
            .Where(AgentFunctionBindingCodec.IsWorkflowBinding))
        {
            var workflowId = ParseWorkflowId(binding);
            if (workflowId <= 0)
            {
                continue;
            }

            if (targetWorkflowId == workflowId)
            {
                return true;
            }

            if (await WorkflowReachesWorkflowOrAgentAsync(
                    workflowId,
                    targetWorkflowId,
                    targetAgentId,
                    adminUserId,
                    new HashSet<string>(path, StringComparer.OrdinalIgnoreCase),
                    cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> GroupReachesWorkflowAsync(
        int groupId,
        int targetWorkflowId,
        int adminUserId,
        HashSet<string> path,
        CancellationToken cancellationToken)
        => await GroupReachesWorkflowOrAgentAsync(
            groupId,
            targetWorkflowId,
            null,
            adminUserId,
            path,
            cancellationToken).ConfigureAwait(false);

    private async Task<bool> GroupReachesWorkflowOrAgentAsync(
        int groupId,
        int? targetWorkflowId,
        int? targetAgentId,
        int adminUserId,
        HashSet<string> path,
        CancellationToken cancellationToken)
    {
        var nodeKey = $"group:{groupId}";
        if (!path.Add(nodeKey))
        {
            return false;
        }

        var group = await _groupService.GetObjectAsync(item => item.Id == groupId).ConfigureAwait(false);
        if (group == null)
        {
            return false;
        }

        var agentIds = (await _memberService.GetFullListAsync(item => item.ChatGroupId == groupId)
                .ConfigureAwait(false))
            .Select(item => item.AgentTemplateId)
            .Concat(new[] { group.AdminAgentTemplateId, group.EnterAgentTemplateId })
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        foreach (var agentId in agentIds)
        {
            if (targetAgentId == agentId)
            {
                return true;
            }

            if (await AgentReachesWorkflowOrAgentAsync(
                    agentId,
                    targetWorkflowId,
                    targetAgentId,
                    adminUserId,
                    new HashSet<string>(path, StringComparer.OrdinalIgnoreCase),
                    cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }

    private static int ParseWorkflowId(AgentFunctionBindingDto binding)
    {
        if (binding?.WorkflowId > 0)
        {
            return binding.WorkflowId.Value;
        }

        return int.TryParse(binding?.Key, out var id) ? id : 0;
    }
}
