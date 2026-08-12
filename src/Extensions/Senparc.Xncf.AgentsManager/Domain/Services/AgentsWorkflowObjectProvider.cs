/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AgentsWorkflowObjectProvider.cs
    文件功能描述：向 NeuChar Workflow 提供 AgentsManager 组和独立 Agent


    创建标识：Senparc - 20260809

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

----------------------------------------------------------------*/

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

public sealed class AgentsWorkflowObjectProvider : IWorkflowObjectProvider
{
    public const string ProviderName = "agents-manager";
    private readonly AgentsTemplateService _agentService;
    private readonly ChatGroupService _groupService;
    private readonly AgentTemplateRunner _agentTemplateRunner;
    private readonly XncfModuleService _moduleService;
    private readonly RemoteAgentService _remoteAgentService;
    private readonly RemoteA2AAgentFactory _remoteA2AAgentFactory;

    public AgentsWorkflowObjectProvider(
        AgentsTemplateService agentService,
        ChatGroupService groupService,
        AgentTemplateRunner agentTemplateRunner,
        XncfModuleService moduleService,
        RemoteAgentService remoteAgentService,
        RemoteA2AAgentFactory remoteA2AAgentFactory)
    {
        _agentService = agentService;
        _groupService = groupService;
        _agentTemplateRunner = agentTemplateRunner;
        _moduleService = moduleService;
        _remoteAgentService = remoteAgentService;
        _remoteA2AAgentFactory = remoteA2AAgentFactory;
    }

    public string ProviderId => ProviderName;

    public async ValueTask<IReadOnlyList<WorkflowObjectDescriptor>> GetObjectsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await IsModuleAvailableAsync().ConfigureAwait(false))
        {
            return Array.Empty<WorkflowObjectDescriptor>();
        }
        var agents = await _agentService.GetFullListAsync(
            z => true,
            z => z.Name,
            OrderingType.Ascending).ConfigureAwait(false);
        var groups = await _groupService.GetFullListAsync(
            z => true,
            z => z.Name,
            OrderingType.Ascending).ConfigureAwait(false);
        var remoteAgents = await _remoteAgentService.GetFullListAsync(
            z => true,
            z => z.Name,
            OrderingType.Ascending).ConfigureAwait(false);

        return agents.Select(z => new WorkflowObjectDescriptor(
                ProviderId,
                $"agent:{z.Id}",
                "agent",
                z.Name,
                z.Description,
                z.Enable,
                "fa fa-user-circle",
                $"/Admin/AgentsManager/Index#tab=first&view=edit&agentId={z.Id}",
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["id"] = z.Id.ToString(),
                    ["type"] = "独立 Agent",
                    ["enabled"] = z.Enable ? "true" : "false",
                    ["promptCode"] = z.PromptCode ?? string.Empty,
                    ["functionCallNames"] = z.FunctionCallNames ?? string.Empty,
                    ["knowledgeBaseId"] = z.KnowledgeBaseId?.ToString() ?? string.Empty
                }))
            .Concat(groups.Select(z => new WorkflowObjectDescriptor(
                ProviderId,
                $"group:{z.Id}",
                "agent-group",
                z.Name,
                z.Description,
                z.Enable,
                "fa fa-users",
                $"/Admin/AgentsManager/Index#tab=second&view=edit&groupId={z.Id}",
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["id"] = z.Id.ToString(),
                    ["type"] = "Agent 组",
                    ["enabled"] = z.Enable ? "true" : "false",
                    ["state"] = z.State.ToString(),
                    ["adminAgentTemplateId"] = z.AdminAgentTemplateId.ToString(),
                    ["enterAgentTemplateId"] = z.EnterAgentTemplateId.ToString()
                })))
            .Concat(remoteAgents.Select(z => new WorkflowObjectDescriptor(
                ProviderId,
                $"a2a:{z.Id}",
                "a2a",
                z.Name,
                z.Description,
                z.Enable,
                "fa fa-exchange",
                $"/Admin/AgentsManager/Index#tab=remoteA2A&view=edit&remoteAgentId={z.Id}",
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["id"] = z.Id.ToString(),
                    ["type"] = "远程 A2A Agent",
                    ["enabled"] = z.Enable ? "true" : "false",
                    ["protocol"] = z.Protocol.ToString(),
                    ["connectionStatus"] = z.ConnectionStatus.ToString(),
                    ["agentCardUrl"] = z.AgentCardUrl ?? string.Empty
                })))
            .ToList();
    }

    public async ValueTask<WorkflowObjectExecutionResult> ExecuteAsync(
        WorkflowObjectExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await IsModuleAvailableAsync().ConfigureAwait(false))
        {
            return new WorkflowObjectExecutionResult(false, null, "AgentsManager 模块未安装或未开启。");
        }
        if (request.ObjectId?.StartsWith("group:", StringComparison.OrdinalIgnoreCase) == true &&
            int.TryParse(request.ObjectId[6..], out var groupId))
        {
            var group = await _groupService.GetObjectAsync(z => z.Id == groupId).ConfigureAwait(false);
            if (group == null || !group.Enable)
            {
                return new WorkflowObjectExecutionResult(false, null, "Agent 组不存在或未启用。");
            }

            await _groupService.RunChatGroupAwaitAsync(new ChatGroup_RunGroupRequest
            {
                ChatGroupId = groupId,
                AiModelId = request.AiModelId,
                PromptCommand = request.Input,
                Name = $"Workflow · {group.Name}",
                Description = $"NeuChar Workflow {request.CorrelationId}",
                Personality = false,
                HookPlatform = HookPlatform.None,
                CorrelationId = request.CorrelationId
            }).ConfigureAwait(false);
            return new WorkflowObjectExecutionResult(true, $"Agent 组“{group.Name}”已完成本轮任务。");
        }

        if (request.ObjectId?.StartsWith("agent:", StringComparison.OrdinalIgnoreCase) == true &&
            int.TryParse(request.ObjectId[6..], out var agentId))
        {
            return await ExecuteSingleAgentAsync(agentId, request, cancellationToken).ConfigureAwait(false);
        }

        if (request.ObjectId?.StartsWith("a2a:", StringComparison.OrdinalIgnoreCase) == true &&
            int.TryParse(request.ObjectId[4..], out var remoteAgentId))
        {
            return await ExecuteRemoteA2AAsync(remoteAgentId, request, cancellationToken).ConfigureAwait(false);
        }

        return new WorkflowObjectExecutionResult(false, null, "无法识别的 AgentsManager 工作流对象。");
    }

    private async ValueTask<WorkflowObjectExecutionResult> ExecuteSingleAgentAsync(
        int agentId,
        WorkflowObjectExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var agent = await _agentService.GetObjectAsync(z => z.Id == agentId).ConfigureAwait(false);
        if (agent == null || !agent.Enable)
        {
            return new WorkflowObjectExecutionResult(false, null, "独立 Agent 不存在或未启用。");
        }

        var execution = await _agentTemplateRunner.RunAsync(
                agent,
                request.Input,
                AgentTemplateRunRequest.ForLocalWorkflow(
                    agentId,
                    request.CorrelationId,
                    request.AiModelId > 0 ? request.AiModelId : null),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return execution.Success
            ? new WorkflowObjectExecutionResult(true, execution.Output)
            : new WorkflowObjectExecutionResult(false, null, execution.ErrorMessage);
    }

    private async ValueTask<WorkflowObjectExecutionResult> ExecuteRemoteA2AAsync(
        int remoteAgentId,
        WorkflowObjectExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var remoteAgent = await _remoteAgentService.GetObjectAsync(z => z.Id == remoteAgentId).ConfigureAwait(false);
        if (remoteAgent == null || !remoteAgent.Enable)
        {
            return new WorkflowObjectExecutionResult(false, null, "远程 A2A Agent 不存在或未启用。");
        }

        try
        {
            var agent = await _remoteA2AAgentFactory.CreateAsync(remoteAgent, cancellationToken).ConfigureAwait(false);
            // A2A errors are emitted on the streaming event channel. Calling RunAsync() here
            // can collapse a remote A2A error into "did not produce any response events", which
            // hides the failure returned by the remote server. Aggregate the same stream used by
            // ChatGroup so callers receive the actual A2A exception and its diagnostic id.
            var response = await agent.RunStreamingAsync(
                    request.Input ?? string.Empty,
                    session: null,
                    options: null,
                    cancellationToken: cancellationToken)
                .ToAgentResponseAsync(cancellationToken)
                .ConfigureAwait(false);
            var output = response?.Text?.Trim();
            return string.IsNullOrWhiteSpace(output)
                ? new WorkflowObjectExecutionResult(false, null, "远程 A2A Agent 没有返回可显示内容。")
                : new WorkflowObjectExecutionResult(true, output);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new WorkflowObjectExecutionResult(false, null, $"远程 A2A Agent 调用失败：{ex.Message}");
        }
    }

    private async Task<bool> IsModuleAvailableAsync()
    {
        var module = await _moduleService.GetObjectAsync(z => z.Uid == Register.ModuleUid).ConfigureAwait(false);
        return module?.State == XncfModules_State.开放;
    }
}
