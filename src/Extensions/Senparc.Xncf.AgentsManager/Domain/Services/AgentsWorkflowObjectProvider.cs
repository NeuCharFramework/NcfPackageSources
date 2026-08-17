/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AgentsWorkflowObjectProvider.cs
    文件功能描述：向 NeuChar Workflow 提供 AgentsManager 组和独立 Agent


    创建标识：Senparc - 20260809

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

    修改标识：Senparc - 20260815
    修改描述：v0.15.0-preview20 增强 AgentTemplate、ChatGroup 与发布型 A2A 的取消和请求处理

    修改标识：Senparc - 20260817
    修改描述：v0.16.0-preview21 支持 Human-in-the-Loop 人工审批与人类参与者执行策略

----------------------------------------------------------------*/

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    private readonly HumanInTheLoopRequestStore _humanInTheLoopRequestStore;
    private readonly AgentsManagerNeuBellProvider _neuBellProvider;

    public AgentsWorkflowObjectProvider(
        AgentsTemplateService agentService,
        ChatGroupService groupService,
        AgentTemplateRunner agentTemplateRunner,
        XncfModuleService moduleService,
        RemoteAgentService remoteAgentService,
        RemoteA2AAgentFactory remoteA2AAgentFactory,
        HumanInTheLoopRequestStore humanInTheLoopRequestStore,
        AgentsManagerNeuBellProvider neuBellProvider)
    {
        _agentService = agentService;
        _groupService = groupService;
        _agentTemplateRunner = agentTemplateRunner;
        _moduleService = moduleService;
        _remoteAgentService = remoteAgentService;
        _remoteA2AAgentFactory = remoteA2AAgentFactory;
        _humanInTheLoopRequestStore = humanInTheLoopRequestStore;
        _neuBellProvider = neuBellProvider;
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
                    ["knowledgeBaseId"] = z.KnowledgeBaseId?.ToString() ?? string.Empty,
                    ["supportsHumanInTheLoop"] = "true",
                    ["supportsHumanParticipant"] = "false"
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
                    ["enterAgentTemplateId"] = z.EnterAgentTemplateId.ToString(),
                    ["supportsHumanInTheLoop"] = "true",
                    ["supportsHumanParticipant"] = "true"
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
                Personality = GetBooleanParameter(
                    request,
                    WorkflowObjectExecutionParameters.Personality,
                    fallback: true),
                HookPlatform = HookPlatform.None,
                CorrelationId = request.CorrelationId,
                HumanRecipientUserId = request.AdminUserId?.ToString(),
                HumanInTheLoopLevel = GetEnumParameter(
                    request,
                    WorkflowObjectExecutionParameters.HumanInTheLoopLevel,
                    HumanInTheLoopLevel.Automatic),
                PluginToolPermission = GetEnumParameter(
                    request,
                    WorkflowObjectExecutionParameters.PluginToolPermission,
                    ToolPermissionMode.Inherit),
                McpToolPermission = GetEnumParameter(
                    request,
                    WorkflowObjectExecutionParameters.McpToolPermission,
                    ToolPermissionMode.Inherit),
                IncludeHumanParticipant = GetBooleanParameter(
                    request,
                    WorkflowObjectExecutionParameters.IncludeHumanParticipant),
                ChatMaxRound = Math.Clamp(
                    GetIntegerParameter(
                        request,
                        WorkflowObjectExecutionParameters.ChatMaxRound,
                        ChatGroupService.ChatMaxRound),
                    1,
                    50),
                CancellationToken = cancellationToken
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

        var allowFunctionCalls = GetBooleanParameter(
            request,
            WorkflowObjectExecutionParameters.AllowFunctionCalls);
        var humanInTheLoopLevel = GetEnumParameter(
            request,
            WorkflowObjectExecutionParameters.HumanInTheLoopLevel,
            HumanInTheLoopLevel.Automatic);
        var pluginToolPermission = GetEnumParameter(
            request,
            WorkflowObjectExecutionParameters.PluginToolPermission,
            ToolPermissionMode.Inherit);
        var mcpToolPermission = GetEnumParameter(
            request,
            WorkflowObjectExecutionParameters.McpToolPermission,
            ToolPermissionMode.Inherit);
        var personality = GetBooleanParameter(
            request,
            WorkflowObjectExecutionParameters.Personality,
            fallback: true);
        var runRequest = AgentTemplateRunRequest.ForLocalWorkflow(
            agentId,
            request.CorrelationId,
            request.AiModelId > 0 ? request.AiModelId : null,
            allowFunctionCalls,
            humanInTheLoopLevel,
            pluginToolPermission,
            mcpToolPermission,
            useTemplateModelSettings: personality);
        var effectivePolicy = HumanInTheLoopPolicyResolver.Resolve(
            humanInTheLoopLevel,
            pluginToolPermission,
            mcpToolPermission);
        if (allowFunctionCalls
            && (effectivePolicy.PluginTools == ToolPermissionMode.RequireApproval
                || effectivePolicy.McpTools == ToolPermissionMode.RequireApproval))
        {
            return await ExecuteSingleAgentWithHumanApprovalAsync(
                agent,
                request,
                runRequest,
                cancellationToken).ConfigureAwait(false);
        }

        var execution = await _agentTemplateRunner.RunAsync(
                agent,
                request.Input,
                runRequest,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return execution.Success
            ? new WorkflowObjectExecutionResult(true, execution.Output)
            : new WorkflowObjectExecutionResult(false, null, execution.ErrorMessage);
    }

    private async ValueTask<WorkflowObjectExecutionResult> ExecuteSingleAgentWithHumanApprovalAsync(
        AgentTemplate agent,
        WorkflowObjectExecutionRequest request,
        AgentTemplateRunRequest runRequest,
        CancellationToken cancellationToken)
    {
        var build = await _agentTemplateRunner.BuildAsync(
            agent,
            request.Input,
            runRequest,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!build.Success)
        {
            return new WorkflowObjectExecutionResult(false, null, build.ErrorMessage);
        }

        var session = build.Runner?.Kernel?.AgentSession;
        var nextMessages = new List<ChatMessage>
        {
            new(ChatRole.User, request.Input ?? string.Empty)
        };
        var output = new StringBuilder();
        var registeredRequests = new List<PendingHumanRequest>();

        try
        {
            while (nextMessages != null)
            {
                var approvals = new List<ToolApprovalRequestContent>();
                await foreach (var update in build.Runner.Kernel.ChatClientAgent.RunStreamingAsync(
                    nextMessages,
                    session,
                    cancellationToken: cancellationToken))
                {
                    if (!string.IsNullOrWhiteSpace(update?.Text))
                    {
                        output.Append(update.Text);
                    }
                    if (update?.Contents != null)
                    {
                        approvals.AddRange(update.Contents.OfType<ToolApprovalRequestContent>());
                    }
                }

                if (approvals.Count == 0)
                {
                    break;
                }

                registeredRequests = approvals
                    .Select(approval => _humanInTheLoopRequestStore.RegisterToolApproval(
                        0,
                        agent.Name,
                        approval,
                        decision => approval.CreateResponse(decision.Approved, decision.Reason),
                        request.CorrelationId,
                        request.AdminUserId?.ToString()))
                    .ToList();

                foreach (var pending in registeredRequests)
                {
                    var itemId = _neuBellProvider.SendWorkflowToolApproval(
                        request.CorrelationId,
                        request.AdminUserId?.ToString(),
                        pending.AgentName,
                        pending.ToolName);
                    pending.SetNeuBellItemId(itemId);
                }
                await NotifyNeuBellChangedAsync().ConfigureAwait(false);

                var responses = new List<ChatMessage>();
                foreach (var pending in registeredRequests)
                {
                    await pending.Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
                    if (pending.ResolvedResponse is ToolApprovalResponseContent approvalResponse)
                    {
                        responses.Add(new ChatMessage(ChatRole.User, new[] { approvalResponse }));
                    }
                }

                registeredRequests.Clear();
                nextMessages = responses;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await CancelPendingRequestsAsync(
                registeredRequests,
                request.AdminUserId?.ToString()).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            await CancelPendingRequestsAsync(
                registeredRequests,
                request.AdminUserId?.ToString()).ConfigureAwait(false);
            return new WorkflowObjectExecutionResult(false, null, $"独立 Agent 执行失败：{ex.Message}");
        }

        var result = output.ToString().Trim();
        return string.IsNullOrWhiteSpace(result)
            ? new WorkflowObjectExecutionResult(false, null, "独立 Agent 没有返回有效内容。")
            : new WorkflowObjectExecutionResult(true, result);
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

    private static bool GetBooleanParameter(
        WorkflowObjectExecutionRequest request,
        string name,
        bool fallback = false)
    {
        return request.Parameters != null
            && request.Parameters.TryGetValue(name, out var value)
            && bool.TryParse(value, out var parsed)
                ? parsed
                : fallback;
    }

    private static TEnum GetEnumParameter<TEnum>(
        WorkflowObjectExecutionRequest request,
        string name,
        TEnum fallback)
        where TEnum : struct, Enum
    {
        if (request.Parameters == null
            || !request.Parameters.TryGetValue(name, out var value)
            || !int.TryParse(value, out var numeric)
            || !Enum.IsDefined(typeof(TEnum), numeric))
        {
            return fallback;
        }

        return (TEnum)Enum.ToObject(typeof(TEnum), numeric);
    }

    private static int GetIntegerParameter(
        WorkflowObjectExecutionRequest request,
        string name,
        int fallback)
    {
        return request.Parameters != null
            && request.Parameters.TryGetValue(name, out var value)
            && int.TryParse(value, out var parsed)
                ? parsed
                : fallback;
    }

    private static async Task NotifyNeuBellChangedAsync()
    {
        var publisher = SenparcDI.GetServiceProvider(true).GetService<INeuBellPublisher>();
        if (publisher != null)
        {
            await publisher.NotifyChangedAsync(AgentsManagerNeuBellProvider.ProviderIdValue).ConfigureAwait(false);
        }
    }

    private async Task CancelPendingRequestsAsync(
        IEnumerable<PendingHumanRequest> requests,
        string recipientUserId)
    {
        var changed = false;
        foreach (var pending in requests ?? Array.Empty<PendingHumanRequest>())
        {
            changed |= _humanInTheLoopRequestStore.TryCancel(pending.RequestId);
            if (!string.IsNullOrWhiteSpace(pending.NeuBellItemId))
            {
                changed |= (await _neuBellProvider.ConsumeItemAsync(
                    new NeuBellRequestContext(recipientUserId),
                    pending.NeuBellItemId).ConfigureAwait(false)) > 0;
            }
        }

        if (changed)
        {
            await NotifyNeuBellChangedAsync().ConfigureAwait(false);
        }
    }
}
