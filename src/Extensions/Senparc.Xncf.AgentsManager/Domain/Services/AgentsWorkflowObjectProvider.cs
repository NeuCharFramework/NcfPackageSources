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
using Senparc.Ncf.Shared.Abstractions.ChatAgent;
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

    public AgentsWorkflowObjectProvider(
        AgentsTemplateService agentService,
        ChatGroupService groupService,
        PromptItemService promptItemService,
        AIModelService aiModelService,
        XncfModuleService moduleService)
    {
        _agentService = agentService;
        _groupService = groupService;
        _promptItemService = promptItemService;
        _aiModelService = aiModelService;
        _moduleService = moduleService;
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

        return agents.Select(z => new WorkflowObjectDescriptor(
                ProviderId,
                $"agent:{z.Id}",
                "agent",
                z.Name,
                z.Description,
                z.Enable,
                "fa fa-user-circle"))
            .Concat(groups.Select(z => new WorkflowObjectDescriptor(
                ProviderId,
                $"group:{z.Id}",
                "agent-group",
                z.Name,
                z.Description,
                true,
                "fa fa-users")))
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
            if (group == null)
            {
                return new WorkflowObjectExecutionResult(false, null, "Agent 组不存在。");
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

    private async Task<bool> IsModuleAvailableAsync()
    {
        var module = await _moduleService.GetObjectAsync(z => z.Uid == Register.ModuleUid).ConfigureAwait(false);
        return module?.State == XncfModules_State.开放;
    }
}
