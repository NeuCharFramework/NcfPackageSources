/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AgentExecutionService.cs
    文件功能描述：独立 Agent 统一执行、记录、SSE 和用量收口服务

    创建标识：Senparc - 20260822

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 统一 Workflow、管理页、外部 API 和发布型 A2A 的执行入口

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 新增独立 Agent 执行任务持久化、管理页和 SSE 过程回放


----------------------------------------------------------------*/

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Domain.Models.Usage;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

public sealed class AgentExecutionService : ServiceBase<AgentExecutionTask>
{
    private static readonly JsonSerializerOptions EventJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AgentsTemplateService _agentTemplateService;
    private readonly AgentTemplateRunner _agentTemplateRunner;
    private readonly HumanInTheLoopRequestStore _humanInTheLoopRequestStore;
    private readonly AgentsManagerNeuBellProvider _neuBellProvider;
    private readonly AgentExecutionStreamHub _streamHub;
    private readonly AgentExecutionRuntimeStore _runtimeStore;

    public AgentExecutionService(
        IRepositoryBase<AgentExecutionTask> repo,
        IServiceProvider serviceProvider,
        AgentsTemplateService agentTemplateService,
        AgentTemplateRunner agentTemplateRunner,
        HumanInTheLoopRequestStore humanInTheLoopRequestStore,
        AgentsManagerNeuBellProvider neuBellProvider,
        AgentExecutionStreamHub streamHub,
        AgentExecutionRuntimeStore runtimeStore)
        : base(repo, serviceProvider)
    {
        _agentTemplateService = agentTemplateService;
        _agentTemplateRunner = agentTemplateRunner;
        _humanInTheLoopRequestStore = humanInTheLoopRequestStore;
        _neuBellProvider = neuBellProvider;
        _streamHub = streamHub;
        _runtimeStore = runtimeStore;
    }

    public async Task<AgentExecutionResult> ExecuteAsync(
        AgentExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agent = await GetAgentAsync(request.AgentTemplateId).ConfigureAwait(false);
        var task = await CreateTaskAsync(request, agent).ConfigureAwait(false);
        return await ExecuteTaskAsync(task, agent, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AgentExecutionTaskDto> StartAsync(AgentExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agent = await GetAgentAsync(request.AgentTemplateId).ConfigureAwait(false);
        var task = await CreateTaskAsync(request, agent).ConfigureAwait(false);
        var taskId = task.Id;
        var cancellationTokenSource = new CancellationTokenSource();
        _runtimeStore.Register(taskId, cancellationTokenSource);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = SenparcDI.GetServiceProvider(true).CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<AgentExecutionService>();
                await service.ExecuteTaskAsync(taskId, cancellationTokenSource.Token).ConfigureAwait(false);
            }
            finally
            {
                _runtimeStore.Remove(taskId);
            }
        });

        return new AgentExecutionTaskDto(task);
    }

    public bool Cancel(int taskId)
    {
        return _runtimeStore.TryCancel(taskId);
    }

    public async Task<AgentExecutionTaskDto> GetTaskDtoAsync(int taskId, bool includeEvents = false)
    {
        var task = await GetObjectAsync(item => item.Id == taskId).ConfigureAwait(false);
        if (task == null)
        {
            return null;
        }

        var dto = new AgentExecutionTaskDto(task);
        if (includeEvents)
        {
            dto.Events = DeserializeEvents(task.EventsJson);
        }

        return dto;
    }

    public async Task<IReadOnlyList<AgentExecutionTaskDto>> GetTaskDtosAsync(
        int agentTemplateId = 0,
        string source = null,
        string filter = null,
        AgentExecutionTask_Status? status = null,
        int pageIndex = 0,
        int pageSize = 20)
    {
        var tasks = await GetObjectListAsync(
                pageIndex,
                pageSize,
                item => (agentTemplateId <= 0 || item.AgentTemplateId == agentTemplateId)
                    && (string.IsNullOrWhiteSpace(source) || item.Source == source)
                    && (string.IsNullOrWhiteSpace(filter) || item.Name.Contains(filter))
                    && (!status.HasValue || item.Status == status.Value),
                item => item.Id,
                Ncf.Core.Enums.OrderingType.Descending)
            .ConfigureAwait(false);

        return tasks.Select(task => new AgentExecutionTaskDto(task)).ToList();
    }

    public async Task<IReadOnlyList<AgentExecutionEventDto>> GetEventsAsync(
        int taskId,
        int afterSequence = 0)
    {
        var task = await GetObjectAsync(item => item.Id == taskId).ConfigureAwait(false);
        return task == null
            ? Array.Empty<AgentExecutionEventDto>()
            : DeserializeEvents(task.EventsJson)
                .Where(item => item.Sequence > afterSequence)
                .ToList();
    }

    private async Task<AgentExecutionResult> ExecuteTaskAsync(
        int taskId,
        CancellationToken cancellationToken)
    {
        var task = await GetObjectAsync(item => item.Id == taskId).ConfigureAwait(false);
        if (task == null)
        {
            return AgentExecutionResult.Failed(taskId, "独立 Agent 执行任务不存在。");
        }

        var agent = await GetAgentAsync(task.AgentTemplateId).ConfigureAwait(false);
        return await ExecuteTaskAsync(task, agent, cancellationToken).ConfigureAwait(false);
    }

    private async Task<AgentExecutionResult> ExecuteTaskAsync(
        AgentExecutionTask task,
        AgentTemplate agent,
        CancellationToken cancellationToken)
    {
        var recorder = new AgentExecutionRecorder(task, _streamHub);
        try
        {
            task.ChangeStatus(AgentExecutionTask_Status.Running);
            await SaveObjectAsync(task).ConfigureAwait(false);
            recorder.Add("status", "running", "独立 Agent 开始执行。", statusOverride: task.Status.ToString());

            var runRequest = BuildRunRequest(task);
            AgentTemplateRunResult execution;
            var effectivePolicy = HumanInTheLoopPolicyResolver.Resolve(
                task.HumanInTheLoopLevel,
                task.PluginToolPermission,
                task.McpToolPermission);
            if (task.AllowFunctionCalls
                && (effectivePolicy.PluginTools == ToolPermissionMode.RequireApproval
                    || effectivePolicy.McpTools == ToolPermissionMode.RequireApproval))
            {
                execution = await ExecuteWithHumanApprovalAsync(
                        task,
                        agent,
                        runRequest,
                        recorder,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                execution = await _agentTemplateRunner.RunAsync(
                        agent,
                        task.PromptCommand,
                        runRequest,
                        diagnostics => recorder.SetModel(diagnostics),
                        cancellationToken,
                        recorder.AddInfo,
                        recorder.AddToolEvent)
                    .ConfigureAwait(false);
            }

            if (execution.Diagnostics != null)
            {
                recorder.SetModel(execution.Diagnostics);
            }

            if (execution.Usage != null)
            {
                var usage = BuildUsage(execution.Usage);
                task.AddUsage(
                    usage.PromptTokens,
                    usage.CompletionTokens,
                    usage.TotalTokens,
                    usage.ResponseMilliseconds);
                recorder.Add(
                    "assistant",
                    "completed",
                    execution.Output,
                    text: execution.Output,
                    responseId: execution.ResponseId,
                    usage: usage);
            }
            else if (!string.IsNullOrWhiteSpace(execution.Output))
            {
                recorder.Add("assistant", "completed", execution.Output, text: execution.Output);
            }

            if (execution.Success)
            {
                task.SetOutput(execution.Output);
                task.ChangeStatus(AgentExecutionTask_Status.Finished);
                recorder.Add("status", "finished", "独立 Agent 执行完成。", statusOverride: task.Status.ToString(), isFinal: true);
            }
            else
            {
                task.SetError(execution.ErrorMessage);
                task.ChangeStatus(AgentExecutionTask_Status.Failed);
                recorder.Add("error", "failed", execution.ErrorMessage, errorMessage: execution.ErrorMessage, statusOverride: task.Status.ToString(), isFinal: true);
            }

            await SaveTaskStateAsync(task, recorder).ConfigureAwait(false);
            return new AgentExecutionResult(
                task.Id,
                execution.Success,
                execution.Output,
                execution.ErrorMessage,
                task.Status,
                execution.Diagnostics);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            task.SetError("独立 Agent 执行已取消。");
            task.ChangeStatus(AgentExecutionTask_Status.Cancelled);
            recorder.Add("status", "cancelled", task.ErrorMessage, errorMessage: task.ErrorMessage, statusOverride: task.Status.ToString(), isFinal: true);
            await SaveTaskStateAsync(task, recorder).ConfigureAwait(false);
            return new AgentExecutionResult(task.Id, false, null, task.ErrorMessage, task.Status);
        }
        catch (Exception ex)
        {
            var root = ex.GetBaseException();
            task.SetError(root.Message);
            task.ChangeStatus(AgentExecutionTask_Status.Failed);
            recorder.Add("error", "failed", root.Message, errorMessage: root.Message, statusOverride: task.Status.ToString(), isFinal: true);
            await SaveTaskStateAsync(task, recorder).ConfigureAwait(false);
            return new AgentExecutionResult(task.Id, false, null, root.Message, task.Status);
        }
    }

    private async Task<AgentExecutionTask> CreateTaskAsync(
        AgentExecutionRequest request,
        AgentTemplate agent)
    {
        var dto = new AgentExecutionTaskDto
        {
            AgentTemplateId = agent.Id,
            AgentTemplateName = agent.Name,
            Name = string.IsNullOrWhiteSpace(request.Name)
                ? $"独立 Agent · {agent.Name}"
                : request.Name.Trim(),
            Source = string.IsNullOrWhiteSpace(request.Source) ? "Direct" : request.Source.Trim(),
            CorrelationId = string.IsNullOrWhiteSpace(request.CorrelationId)
                ? Guid.NewGuid().ToString("N")
                : request.CorrelationId.Trim(),
            ExternalReference = request.ExternalReference,
            WorkflowId = request.WorkflowId,
            AdminUserId = request.AdminUserId,
            AiModelId = request.AiModelId > 0 ? request.AiModelId : null,
            PromptCommand = request.Input ?? string.Empty,
            Status = AgentExecutionTask_Status.Waiting,
            AllowFunctionCalls = request.AllowFunctionCalls,
            HumanInTheLoopLevel = request.HumanInTheLoopLevel,
            PluginToolPermission = request.PluginToolPermission,
            McpToolPermission = request.McpToolPermission,
            IsPersonality = request.UseTemplateModelSettings,
            StartTime = DateTime.Now
        };
        var task = new AgentExecutionTask(dto);
        await SaveObjectAsync(task).ConfigureAwait(false);
        return task;
    }

    private async Task<AgentTemplate> GetAgentAsync(int agentTemplateId)
    {
        if (agentTemplateId <= 0)
        {
            throw new InvalidOperationException("未选择有效的独立 Agent。");
        }

        var agent = await _agentTemplateService
            .GetObjectAsync(item => item.Id == agentTemplateId)
            .ConfigureAwait(false);
        if (agent == null || !agent.Enable)
        {
            throw new InvalidOperationException("独立 Agent 不存在或未启用。");
        }

        return agent;
    }

    private AgentTemplateRunRequest BuildRunRequest(AgentExecutionTask task)
    {
        return new AgentTemplateRunRequest
        {
            ProfileName = $"independent-{task.Source}",
            AiModelId = task.AiModelId,
            AdminUserId = task.AdminUserId,
            AllowFunctionCalls = task.AllowFunctionCalls,
            HumanInTheLoopLevel = task.HumanInTheLoopLevel,
            PluginToolPermission = task.PluginToolPermission,
            McpToolPermission = task.McpToolPermission,
            UseTemplateModelSettings = task.IsPersonality,
            UseTemplatePromptParameters = task.IsPersonality,
            UseFreshAgentSession = true,
            RunnerName = $"AgentExecution-{task.Id}-{task.CorrelationId}",
            DiagnosticId = task.CorrelationId
        };
    }

    private async Task<AgentTemplateRunResult> ExecuteWithHumanApprovalAsync(
        AgentExecutionTask task,
        AgentTemplate agent,
        AgentTemplateRunRequest runRequest,
        AgentExecutionRecorder recorder,
        CancellationToken cancellationToken)
    {
        var build = await _agentTemplateRunner.BuildAsync(
                agent,
                task.PromptCommand,
                runRequest,
                diagnostics => recorder.SetModel(diagnostics),
                recorder.AddInfo,
                cancellationToken,
                recorder.AddToolEvent)
            .ConfigureAwait(false);
        if (!build.Success)
        {
            return AgentTemplateRunResult.Failed(build.ErrorMessage, build.Diagnostics);
        }

        var session = build.Runner?.Kernel?.AgentSession;
        var nextMessages = new List<ChatMessage>
        {
            new(ChatRole.User, task.PromptCommand ?? string.Empty)
        };
        var output = new System.Text.StringBuilder();
        var registeredRequests = new List<PendingHumanRequest>();
        UsageDetails usage = null;

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
                        if (update.Contents.FirstOrDefault(item => item is UsageContent) is UsageContent usageContent
                            && usageContent.Details != null)
                        {
                            usage = usageContent.Details;
                        }
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
                        task.CorrelationId,
                        task.AdminUserId > 0 ? task.AdminUserId.ToString() : null,
                        task.Id))
                    .ToList();

                foreach (var pending in registeredRequests)
                {
                    recorder.Add(
                        "tool-approval",
                        "waiting",
                        $"等待人工审批工具“{pending.ToolName}”。",
                        toolName: pending.ToolName,
                        toolArguments: pending.ToolArguments);
                    var itemId = _neuBellProvider.SendWorkflowToolApproval(
                        task.CorrelationId,
                        task.AdminUserId > 0 ? task.AdminUserId.ToString() : null,
                        pending.AgentName,
                        pending.ToolName);
                    pending.SetNeuBellItemId(itemId);
                }
                task.ChangeStatus(AgentExecutionTask_Status.Paused);
                await SaveTaskStateAsync(task, recorder).ConfigureAwait(false);
                await NotifyNeuBellChangedAsync().ConfigureAwait(false);

                var responses = new List<ChatMessage>();
                foreach (var pending in registeredRequests)
                {
                    await pending.Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
                    recorder.Add(
                        "tool-approval",
                        pending.ResolvedResponse is ToolApprovalResponseContent ? "resolved" : "rejected",
                        $"人工审批已处理工具“{pending.ToolName}”。",
                        toolName: pending.ToolName);
                    if (pending.ResolvedResponse is ToolApprovalResponseContent approvalResponse)
                    {
                        responses.Add(new ChatMessage(ChatRole.User, new[] { approvalResponse }));
                    }
                }

                registeredRequests.Clear();
                nextMessages = responses;
                task.ChangeStatus(AgentExecutionTask_Status.Running);
                await SaveTaskStateAsync(task, recorder).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await CancelPendingRequestsAsync(registeredRequests, task.AdminUserId).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            await CancelPendingRequestsAsync(registeredRequests, task.AdminUserId).ConfigureAwait(false);
            return AgentTemplateRunResult.Failed(
                $"独立 Agent 执行失败：{ex.Message}",
                build.Diagnostics,
                usage);
        }

        var result = output.ToString().Trim();
        return string.IsNullOrWhiteSpace(result)
            ? AgentTemplateRunResult.Failed("独立 Agent 没有返回有效内容。", build.Diagnostics, usage)
            : AgentTemplateRunResult.Succeeded(result, build.Diagnostics, usage);
    }

    private async Task CancelPendingRequestsAsync(
        IEnumerable<PendingHumanRequest> requests,
        int adminUserId)
    {
        var changed = false;
        foreach (var pending in requests ?? Array.Empty<PendingHumanRequest>())
        {
            changed |= _humanInTheLoopRequestStore.TryCancel(pending.RequestId);
            if (!string.IsNullOrWhiteSpace(pending.NeuBellItemId))
            {
                changed |= (await _neuBellProvider.ConsumeItemAsync(
                    new NeuBellRequestContext(adminUserId > 0 ? adminUserId.ToString() : null),
                    pending.NeuBellItemId).ConfigureAwait(false)) > 0;
            }
        }

        if (changed)
        {
            await NotifyNeuBellChangedAsync().ConfigureAwait(false);
        }
    }

    private static async Task NotifyNeuBellChangedAsync()
    {
        var publisher = SenparcDI.GetServiceProvider(true).GetService<INeuBellPublisher>();
        if (publisher != null)
        {
            await publisher.NotifyChangedAsync(AgentsManagerNeuBellProvider.ProviderIdValue).ConfigureAwait(false);
        }
    }

    private async Task SaveTaskStateAsync(
        AgentExecutionTask task,
        AgentExecutionRecorder recorder)
    {
        task.SetEvents(recorder.SerializeEvents());
        await SaveObjectAsync(task).ConfigureAwait(false);
    }

    private static ChatUsageSnapshot BuildUsage(UsageDetails usage)
    {
        var promptTokens = ClampToInt(usage.InputTokenCount ?? 0);
        var completionTokens = ClampToInt(usage.OutputTokenCount ?? 0);
        var totalTokens = ClampToInt(usage.TotalTokenCount ?? 0);
        return new ChatUsageSnapshot
        {
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            TotalTokens = totalTokens > 0 ? totalTokens : promptTokens + completionTokens,
            ResponseMilliseconds = 0,
            RoundIndex = 1
        };
    }

    private static int ClampToInt(long value)
        => value <= 0 ? 0 : value > int.MaxValue ? int.MaxValue : (int)value;

    private static List<AgentExecutionEventDto> DeserializeEvents(string eventsJson)
    {
        if (string.IsNullOrWhiteSpace(eventsJson))
        {
            return new List<AgentExecutionEventDto>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<AgentExecutionEventDto>>(eventsJson, EventJsonOptions)
                ?? new List<AgentExecutionEventDto>();
        }
        catch (JsonException)
        {
            return new List<AgentExecutionEventDto>();
        }
    }

    private sealed class AgentExecutionRecorder
    {
        private readonly object _sync = new();
        private readonly AgentExecutionStreamHub _streamHub;
        private int _sequence;

        public AgentExecutionRecorder(AgentExecutionTask task, AgentExecutionStreamHub streamHub)
        {
            Task = task;
            _streamHub = streamHub;
        }

        private AgentExecutionTask Task { get; }
        private int TaskId => Task.Id;
        public List<AgentExecutionEventDto> Events { get; } = new();

        public string SerializeEvents()
        {
            lock (_sync)
            {
                return JsonSerializer.Serialize(Events, EventJsonOptions);
            }
        }

        public void SetModel(AgentTemplateExecutionDiagnostics diagnostics)
        {
            if (diagnostics == null)
            {
                return;
            }

            Add(
                "model",
                "prepared",
                diagnostics.ModelDescription,
                messageOverride: $"{diagnostics.ExecutionProfile}; {diagnostics.ExecutionParameters}",
                text: diagnostics.ModelDescription);
        }

        public void AddInfo(string message)
        {
            Add("info", "completed", message);
        }

        public void AddToolEvent(AgentToolExecutionEvent item)
        {
            if (item == null)
            {
                return;
            }

            if (string.Equals(item.EventType, "tool-start", StringComparison.OrdinalIgnoreCase))
            {
                Task.AddToolCall();
            }

            Add(
                item.EventType,
                item.Status,
                item.Message,
                toolName: item.ToolName,
                toolArguments: item.Arguments,
                toolResult: item.Result,
                errorMessage: item.ErrorMessage);
        }

        public void Add(
            string eventType,
            string status,
            string message,
            string toolName = null,
            string toolArguments = null,
            string toolResult = null,
            string errorMessage = null,
            string responseId = null,
            string text = null,
            string statusOverride = null,
            string messageOverride = null,
            ChatUsageSnapshot usage = null,
            bool isFinal = false,
            string error = null)
        {
            AgentExecutionEventDto item;
            lock (_sync)
            {
                item = new AgentExecutionEventDto
                {
                    Sequence = ++_sequence,
                    EventType = eventType,
                    Status = statusOverride ?? status,
                    Message = messageOverride ?? message,
                    ToolName = toolName,
                    ToolArguments = toolArguments,
                    ToolResult = toolResult,
                    ErrorMessage = errorMessage ?? error,
                    ResponseId = responseId,
                    Text = text,
                    PromptTokens = usage?.PromptTokens ?? 0,
                    CompletionTokens = usage?.CompletionTokens ?? 0,
                    TotalTokens = usage?.TotalTokens ?? 0,
                    ResponseMilliseconds = usage?.ResponseMilliseconds ?? 0,
                    Timestamp = DateTimeOffset.Now
                };
                Events.Add(item);
            }
            _streamHub.Publish(new AgentExecutionStreamEvent
            {
                AgentExecutionTaskId = TaskId,
                Sequence = item.Sequence,
                EventType = item.EventType,
                Status = item.Status,
                Message = item.Message,
                ToolName = item.ToolName,
                ToolArguments = item.ToolArguments,
                ToolResult = item.ToolResult,
                ErrorMessage = item.ErrorMessage,
                ResponseId = item.ResponseId,
                Text = item.Text,
                PromptTokens = item.PromptTokens,
                CompletionTokens = item.CompletionTokens,
                TotalTokens = item.TotalTokens,
                ResponseMilliseconds = item.ResponseMilliseconds,
                IsFinal = isFinal,
                Timestamp = item.Timestamp
            });
        }
    }
}

public sealed class AgentExecutionRequest
{
    public int AgentTemplateId { get; init; }
    public string Name { get; init; }
    public string Input { get; init; }
    public string Source { get; init; }
    public string CorrelationId { get; init; }
    public string ExternalReference { get; init; }
    public int? WorkflowId { get; init; }
    public int AdminUserId { get; init; }
    public int? AiModelId { get; init; }
    public bool AllowFunctionCalls { get; init; }
    public HumanInTheLoopLevel HumanInTheLoopLevel { get; init; } = HumanInTheLoopLevel.Automatic;
    public ToolPermissionMode PluginToolPermission { get; init; } = ToolPermissionMode.Inherit;
    public ToolPermissionMode McpToolPermission { get; init; } = ToolPermissionMode.Inherit;
    public bool UseTemplateModelSettings { get; init; } = true;
}

public sealed record AgentExecutionResult(
    int TaskId,
    bool Success,
    string Output,
    string ErrorMessage,
    AgentExecutionTask_Status Status,
    AgentTemplateExecutionDiagnostics Diagnostics = null)
{
    public static AgentExecutionResult Failed(int taskId, string errorMessage)
        => new(taskId, false, null, errorMessage, AgentExecutionTask_Status.Failed);
}
