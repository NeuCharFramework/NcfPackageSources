/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AgentsWorkflowObjectProvider.cs
    文件功能描述：向 NeuChar Workflow 提供 AgentsManager 组和独立 Agent
----------------------------------------------------------------*/

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Senparc.AI;
using Senparc.AI.AgentKernel;
using Senparc.AI.AgentKernel.Extensions;
using Senparc.AI.AgentKernel.Handlers;
using Senparc.AI.AgentKernel.IWantToExtensions;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.Domain.Services;
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
    private readonly PromptItemService _promptItemService;
    private readonly AIModelService _aiModelService;
    private readonly XncfModuleService _moduleService;
    private readonly RemoteAgentService _remoteAgentService;
    private readonly RemoteA2AAgentFactory _remoteA2AAgentFactory;

    public AgentsWorkflowObjectProvider(
        AgentsTemplateService agentService,
        ChatGroupService groupService,
        PromptItemService promptItemService,
        AIModelService aiModelService,
        XncfModuleService moduleService,
        RemoteAgentService remoteAgentService,
        RemoteA2AAgentFactory remoteA2AAgentFactory)
    {
        _agentService = agentService;
        _groupService = groupService;
        _promptItemService = promptItemService;
        _aiModelService = aiModelService;
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
                "/Admin/AgentsManager/Index#tab=remoteA2A",
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

        var setting = Senparc.AI.Config.SenparcAiSetting as SenparcAiSetting;
        var promptContent = string.Empty;
        if (!string.IsNullOrWhiteSpace(agent.PromptCode))
        {
            var promptItem = await _promptItemService.GetBestPromptAsync(agent.PromptCode, true).ConfigureAwait(false);
            if (promptItem != null)
            {
                promptContent = promptItem.Content ?? string.Empty;
                var modelId = request.AiModelId > 0 ? request.AiModelId : promptItem.ModelId;
                if (modelId > 0)
                {
                    var aiModel = await _aiModelService.GetObjectAsync(z => z.Id == modelId).ConfigureAwait(false);
                    if (aiModel != null)
                    {
                        setting = _aiModelService.BuildSenparcAiSetting(
                            _aiModelService.Mapper.Map<AIModelDto>(aiModel));
                    }
                }
            }
        }

        if (setting == null || setting.AiPlatform == AiPlatform.UnSet)
        {
            return new WorkflowObjectExecutionResult(false, null, "没有可用于独立 Agent 的 Chat 模型。");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var handler = new AgentAiHandler(setting);
#pragma warning disable MEAI001
        var runner = await handler.IWantTo(setting).ConfigChatModel(
            $"WorkflowAgent-{agentId}-{request.CorrelationId}",
            new ChatClientAgentOptions
            {
                ChatOptions = new ChatOptions
                {
                    Instructions = string.Join("\n\n", new[] { agent.SystemMessage, promptContent }.Where(z => !string.IsNullOrWhiteSpace(z))),
                    MaxOutputTokens = 3000,
                    Temperature = 0.5f
                }
            }).BuildKernelWithAgentSessionAsync().ConfigureAwait(false);
#pragma warning restore MEAI001
        var result = await runner.RunChatAsync(request.Input ?? string.Empty).ConfigureAwait(false);
        var output = result?.OutputString?.Trim();
        return string.IsNullOrWhiteSpace(output)
            ? new WorkflowObjectExecutionResult(false, null, "独立 Agent 没有返回有效内容。")
            : new WorkflowObjectExecutionResult(true, output);
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
