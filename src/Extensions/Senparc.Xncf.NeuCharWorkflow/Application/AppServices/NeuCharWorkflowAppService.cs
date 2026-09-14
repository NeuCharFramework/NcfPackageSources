/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NeuCharWorkflowAppService.cs
    文件功能描述：增强工作流编排、回放、Webhook 与并行执行能力


    创建标识：Senparc - 20260810

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

    修改标识：Senparc - 20260815
    修改描述：v0.2.0 增强工作流并行与运行控制

    修改标识：Senparc - 20260817
    修改描述：v0.2.0 支持 Human Input 人工节点暂停与外部恢复

    修改标识：Senparc - 20260822
    修改描述：v0.2.0 增强工作流函数调用、任务控制与回放管理

    修改标识：Senparc - 20260829
    修改描述：v0.3.0 新增工作流分析查询与管理端可视化

    修改标识：Senparc - 20260909
    修改描述：v0.4.0 新增 Chat 触发器：聊天会话、消息发送与运行状态查询

    修改标识：Senparc - 20260913
    修改描述：v0.4.0 Chat 历史落库持久化，支持跨主机重启恢复

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Senparc.Xncf.AgentsManager.Abstractions;
using Senparc.Xncf.NeuCharWorkflow.Application.Events;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel;
using Senparc.Xncf.NeuCharWorkflow.Domain.Services;
using WorkflowEntity = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Application.AppServices;

/// <summary>
/// Workflow 的应用边界：页面与 HTTP 适配器只能通过此服务读取、保存、校验和执行工作流，
/// 不直接持有仓储或领域执行器。
/// </summary>
public sealed class NeuCharWorkflowAppService
{
    private const int TaskListPageSize = 30;
    private static readonly TimeSpan PersistedRunRecoveryAge = TimeSpan.FromMinutes(10);
    private static readonly JsonSerializerOptions DesignerJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly NeuCharWorkflowService _workflowService;
    private readonly NeuCharWorkflowVersionService _workflowVersionService;
    private readonly NeuCharWorkflowExecutionLogService _executionLogService;
    private readonly NeuCharWorkflowEngine _workflowEngine;
    private readonly NeuCharWorkflowFunctionService _functionService;
    private readonly NeuCharWorkflowAnalyticsService _analyticsService;
    private readonly NeuCharWorkflowRunCoordinator _runCoordinator;
    private readonly WorkflowEventPublisher _eventPublisher;
    private readonly XncfModuleService _xncfModuleService;
    private readonly IWorkflowHumanInteractionBridge _humanInteractionBridge;
    private readonly NeuCharWorkflowHumanInputService _humanInputService;
    private readonly NeuCharWorkflowChatSessionService _chatSessionService;
    private readonly NeuCharWorkflowChatMessageService _chatMessageService;
    private readonly IAgentWorkflowReferenceValidator? _agentWorkflowReferenceValidator;
    private readonly ILogger<NeuCharWorkflowAppService> _logger;

    public NeuCharWorkflowAppService(
        NeuCharWorkflowService workflowService,
        NeuCharWorkflowVersionService workflowVersionService,
        NeuCharWorkflowExecutionLogService executionLogService,
        NeuCharWorkflowEngine workflowEngine,
        NeuCharWorkflowFunctionService functionService,
        NeuCharWorkflowAnalyticsService analyticsService,
        NeuCharWorkflowRunCoordinator runCoordinator,
        WorkflowEventPublisher eventPublisher,
        XncfModuleService xncfModuleService,
        IWorkflowHumanInteractionBridge humanInteractionBridge,
        NeuCharWorkflowHumanInputService humanInputService,
        NeuCharWorkflowChatSessionService chatSessionService,
        NeuCharWorkflowChatMessageService chatMessageService,
        IEnumerable<IAgentWorkflowReferenceValidator>? agentWorkflowReferenceValidators = null,
        ILogger<NeuCharWorkflowAppService>? logger = null)
    {
        _workflowService = workflowService;
        _workflowVersionService = workflowVersionService;
        _executionLogService = executionLogService;
        _workflowEngine = workflowEngine;
        _functionService = functionService;
        _analyticsService = analyticsService;
        _runCoordinator = runCoordinator;
        _eventPublisher = eventPublisher;
        _xncfModuleService = xncfModuleService;
        _humanInteractionBridge = humanInteractionBridge;
        _humanInputService = humanInputService;
        _chatSessionService = chatSessionService;
        _chatMessageService = chatMessageService;
        _logger = logger ?? NullLogger<NeuCharWorkflowAppService>.Instance;
        _agentWorkflowReferenceValidator = agentWorkflowReferenceValidators?.FirstOrDefault();
    }

    public async Task<IReadOnlyList<WorkflowListItem>> GetListAsync(int adminUserId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workflows = await _workflowService.GetFullListAsync(
            z => z.AdminUserId == adminUserId,
            z => z.LastUpdateTime,
            OrderingType.Descending).ConfigureAwait(false);
        return workflows.Select(ToListItem).ToList();
    }

    public async Task<WorkflowDetail?> GetDetailAsync(int workflowId, int adminUserId, CancellationToken cancellationToken = default)
    {
        var workflow = await GetOwnedWorkflowAsync(workflowId, adminUserId).ConfigureAwait(false);
        if (workflow == null)
        {
            return null;
        }
        var editableGraphTask = _workflowEngine.BuildEditableGraphJsonAsync(
            workflow.GraphJson,
            cancellationToken);
        var observedSchemasTask = GetObservedOutputSchemasAsync(workflow.Id);
        await Task.WhenAll(editableGraphTask, observedSchemasTask).ConfigureAwait(false);
        var editableGraphJson = await editableGraphTask.ConfigureAwait(false);
        var observedSchemas = await observedSchemasTask.ConfigureAwait(false);
        var runningCount = await GetRunningCountAsync(
            adminUserId,
            workflow.Id,
            cancellationToken).ConfigureAwait(false);
        return ToDetail(
            workflow,
            editableGraphJson,
            runningCount: runningCount,
            observedOutputSchemas: observedSchemas);
    }

    public async Task<WorkflowDesignerData> GetDesignerDataAsync(CancellationToken cancellationToken = default)
    {
        var catalog = await _functionService.GetCatalogAsync(null, true, cancellationToken).ConfigureAwait(false);
        var objects = await _workflowEngine.GetWorkflowObjectsAsync(cancellationToken).ConfigureAwait(false);
        var functions = catalog.Select(descriptor =>
        {
            var parameterSchema = WorkflowFunctionSchemaBuilder.Build(descriptor);
            return new WorkflowDesignerFunction(
                descriptor.FunctionKey,
                descriptor.Name,
                descriptor.Description,
                descriptor.ModuleUid,
                descriptor.ModuleName,
                descriptor.ModuleVersion,
                descriptor.ModuleAvailable,
                descriptor.ModuleAvailable ? "open" : "disabled",
                JsonSerializer.Serialize(parameterSchema, DesignerJsonOptions),
                JsonSerializer.Serialize(WorkflowFunctionSchemaBuilder.BuildDefaults(parameterSchema), DesignerJsonOptions),
                descriptor.Output,
                descriptor.CatalogError);
        }).ToList();
        return new WorkflowDesignerData(functions, objects);
    }

    public async Task<WorkflowDetail> SaveAsync(
        SaveWorkflowCommand request,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureModuleEnabledAsync().ConfigureAwait(false);
        ValidateSaveCommand(request);
        NeuCharWorkflowGraph graph;
        try
        {
            graph = _workflowEngine.ParseAndValidateGraph(request.GraphJson, requireAllNodesReachable: false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException)
        {
            throw new WorkflowInputException(ex.Message, ex);
        }

        var workflow = request.Id > 0
            ? await GetOwnedWorkflowAsync(request.Id, adminUserId).ConfigureAwait(false)
            : null;
        if (request.Id > 0 && workflow == null)
        {
            throw new WorkflowNotFoundException();
        }
        if (workflow != null && request.ExpectedRevision.HasValue &&
            request.ExpectedRevision.Value != workflow.Revision)
        {
            throw new WorkflowConflictException("工作流已被其他页面更新，请刷新后再保存。");
        }

        try
        {
            await _workflowEngine.MergeExistingSecretsAsync(graph, workflow?.GraphJson, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            throw new WorkflowInputException(ex.Message, ex);
        }

        var hasDisconnectedNodes = _workflowEngine.GetDisconnectedNodes(graph).Count > 0;
        // 未连接节点属于草稿；保留它们以便继续编辑，但不要让定时或 Webhook 触发半成品。
        var enabled = request.Enabled && !hasDisconnectedNodes;
        var subWorkflowError = await _workflowEngine.ValidateSubWorkflowReferencesAsync(
            graph,
            workflow?.Id ?? 0,
            adminUserId,
            requireEnabled: enabled,
            cancellationToken).ConfigureAwait(false);
        if (subWorkflowError != null)
        {
            throw new WorkflowInputException(subWorkflowError);
        }

        if (_agentWorkflowReferenceValidator != null && workflow?.Id > 0)
        {
            var agentReferences = GetAgentWorkflowReferences(graph);
            var agentReferenceError = await _agentWorkflowReferenceValidator
                .ValidateWorkflowReferencesAsync(
                    workflow.Id,
                    adminUserId,
                    agentReferences,
                    cancellationToken)
                .ConfigureAwait(false);
            if (agentReferenceError != null)
            {
                throw new WorkflowInputException(agentReferenceError);
            }
        }
        if (enabled)
        {
            var referenceError = await _workflowEngine.ValidateReferencesAsync(graph, cancellationToken).ConfigureAwait(false);
            if (referenceError != null)
            {
                throw new WorkflowInputException(referenceError);
            }
        }

        var triggerType = request.TriggerType?.Trim().ToLowerInvariant() switch
        {
            "interval" => "interval",
            "webhook" => "webhook",
            "chat" => "chat",
            "manual" or null or "" => "manual",
            _ => null
        };
        if (triggerType == null)
        {
            throw new WorkflowInputException("工作流触发方式无效。");
        }
        if (!graph.Nodes.Any(z => string.Equals(z.Type, $"{triggerType}-trigger", StringComparison.OrdinalIgnoreCase)))
        {
            throw new WorkflowInputException("工作流触发器节点与触发方式不一致。");
        }

        try
        {
            await _workflowEngine.ProtectSecretsAsync(graph, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            throw new WorkflowInputException(ex.Message, ex);
        }

        var normalizedGraphJson = JsonSerializer.Serialize(graph, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        workflow ??= new WorkflowEntity(request.Name.Trim(), adminUserId);
        string triggerConfigJson;
        try
        {
            triggerConfigJson = triggerType switch
            {
                "webhook" => NeuCharWorkflowWebhookConfig.Normalize(
                    request.TriggerConfigJson,
                    workflow.TriggerConfigJson).ToJson(),
                "chat" => NeuCharWorkflowChatConfig.Normalize(
                    request.TriggerConfigJson,
                    workflow.TriggerConfigJson).ToJson(),
                "interval" => request.TriggerConfigJson ?? "{}",
                _ => "{}"
            };
        }
        catch (InvalidOperationException ex)
        {
            throw new WorkflowInputException(ex.Message, ex);
        }

        var nextRun = enabled
            ? NeuCharWorkflowEngine.CalculateNextRun(triggerType, triggerConfigJson, DateTime.UtcNow)
            : null;
        var autoSaveMinutes = request.AutoSaveMinutes <= 0 ? 0 : Math.Clamp(request.AutoSaveMinutes, 1, 1440);
        if (workflow.Id > 0 && IsUnchanged(workflow, request, normalizedGraphJson, enabled, triggerType, triggerConfigJson, autoSaveMinutes))
        {
            var editableUnchanged = await _workflowEngine.BuildEditableGraphJsonAsync(workflow.GraphJson, cancellationToken)
                .ConfigureAwait(false);
            return ToDetail(
                workflow,
                editableUnchanged,
                unchanged: true,
                runningCount: await GetRunningCountAsync(
                    adminUserId,
                    workflow.Id,
                    cancellationToken).ConfigureAwait(false));
        }

        workflow.Update(request.Name, request.Description, normalizedGraphJson, enabled, triggerType,
            triggerConfigJson, nextRun, autoSaveMinutes);
        await _workflowService.SaveObjectAsync(workflow).ConfigureAwait(false);
        await SaveVersionAsync(workflow, adminUserId, request.SaveSource).ConfigureAwait(false);
        await _eventPublisher.PublishAsync(workflow.Id, "saved", adminUserId, cancellationToken).ConfigureAwait(false);
        var editableGraphJson = await _workflowEngine.BuildEditableGraphJsonAsync(workflow.GraphJson, cancellationToken)
            .ConfigureAwait(false);
        return ToDetail(
            workflow,
            editableGraphJson,
            runningCount: await GetRunningCountAsync(
                adminUserId,
                workflow.Id,
                cancellationToken).ConfigureAwait(false));
    }

    public async Task<NeuCharWorkflowRunResult> RunImmediatelyAsync(
        int workflowId,
        int adminUserId,
        string input,
        CancellationToken cancellationToken = default,
        bool parseInputAsJson = false)
    {
        await EnsureModuleEnabledAsync().ConfigureAwait(false);
        ValidateRunInput(workflowId, input);
        var workflow = await GetOwnedWorkflowAsync(workflowId, adminUserId).ConfigureAwait(false)
            ?? throw new WorkflowNotFoundException();
        await ValidateWorkflowAsync(workflow, cancellationToken).ConfigureAwait(false);
        workflow.MarkStarted(workflow.NextRunAt);
        await _workflowService.SaveRuntimeStartedAsync(workflow).ConfigureAwait(false);
        var result = await _workflowEngine.RunAsync(
            workflow,
            input,
            cancellationToken,
            parseInputAsJson: parseInputAsJson).ConfigureAwait(false);
        workflow.MarkCompleted(result.Success, result.ErrorMessage);
        await _workflowService.SaveRuntimeCompletedAsync(workflow).ConfigureAwait(false);
        return result;
    }

    public async Task ValidateRunAsync(int workflowId, int adminUserId, string input, CancellationToken cancellationToken = default)
    {
        await EnsureModuleEnabledAsync().ConfigureAwait(false);
        ValidateRunInput(workflowId, input);
        var workflow = await GetOwnedWorkflowAsync(workflowId, adminUserId).ConfigureAwait(false)
            ?? throw new WorkflowNotFoundException();
        await ValidateWorkflowAsync(workflow, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Guid> StartRunAsync(int workflowId, int adminUserId, string input, CancellationToken cancellationToken = default)
    {
        await ValidateRunAsync(workflowId, adminUserId, input, cancellationToken).ConfigureAwait(false);
        if (!_runCoordinator.TryStart(workflowId, adminUserId, input, out var runId, out var error, "manual"))
        {
            throw new WorkflowConflictException(error);
        }
        return runId;
    }

    public async Task<NeuCharWorkflowRunSnapshot?> GetRunStatusAsync(
        Guid runId,
        int adminUserId,
        long afterSequence,
        CancellationToken cancellationToken = default)
    {
        if (runId == Guid.Empty)
        {
            return null;
        }

        var snapshot = _runCoordinator.GetSnapshot(runId, adminUserId, afterSequence);
        if (snapshot == null)
        {
            return null;
        }

        var correlationId = BuildRunCorrelationId(snapshot.WorkflowId, runId);
        var nativeInteractions = _humanInputService.GetPendingInteractions(
            correlationId,
            adminUserId.ToString());
        var agentInteractions = await _humanInteractionBridge.GetPendingAsync(
            correlationId,
            adminUserId.ToString(),
            cancellationToken).ConfigureAwait(false);
        var interactions = nativeInteractions
            .Concat(agentInteractions)
            .OrderBy(interaction => interaction.CreatedAt)
            .ToList();
        return snapshot with { HumanInteractions = interactions };
    }

    public NeuCharWorkflowRunSnapshot? GetRunStatus(Guid runId, int adminUserId, long afterSequence) =>
        runId == Guid.Empty ? null : _runCoordinator.GetSnapshot(runId, adminUserId, afterSequence);

    public async Task<WorkflowHumanInteractionResult> ResolveHumanInteractionAsync(
        Guid runId,
        int adminUserId,
        string requestId,
        bool approved,
        string input = null,
        string reason = null,
        CancellationToken cancellationToken = default)
    {
        if (runId == Guid.Empty)
        {
            return new WorkflowHumanInteractionResult(false, false, null, null, "缺少有效的 Workflow 运行 ID。");
        }

        var snapshot = _runCoordinator.GetSnapshot(runId, adminUserId, 0);
        if (snapshot == null)
        {
            return new WorkflowHumanInteractionResult(false, false, null, null, "Workflow 运行不存在或不属于当前账号。");
        }

        var correlationId = BuildRunCorrelationId(snapshot.WorkflowId, runId);
        var nativeResolution = await _humanInputService.ResolveForAdminAsync(
            correlationId,
            adminUserId.ToString(),
            requestId,
            approved,
            input,
            reason,
            cancellationToken).ConfigureAwait(false);
        if (nativeResolution.Handled)
        {
            return new WorkflowHumanInteractionResult(
                nativeResolution.Success,
                nativeResolution.Approved,
                nativeResolution.Input,
                nativeResolution.Reason,
                nativeResolution.Message);
        }

        return await _humanInteractionBridge.ResolveAsync(
            correlationId,
            adminUserId.ToString(),
            requestId,
            approved,
            input,
            reason,
            cancellationToken).ConfigureAwait(false);
    }

    public static string BuildRunCorrelationId(int workflowId, Guid runId)
        => $"workflow-{workflowId}-run-{runId:N}";

    public async Task AbortRunAsync(
        Guid? runId,
        int? executionLogId,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        var validRunId = runId.GetValueOrDefault();
        if (validRunId == Guid.Empty && (!executionLogId.HasValue || executionLogId.Value <= 0))
        {
            throw new WorkflowInputException("中止请求缺少有效的运行 ID。");
        }

        if (validRunId != Guid.Empty &&
            _runCoordinator.TryAbort(validRunId, adminUserId, out _))
        {
            return;
        }

        var persistedExecutionLogId = executionLogId.GetValueOrDefault();
        var executionLog = persistedExecutionLogId > 0
            ? await _executionLogService.GetUnfinishedByIdAsync(persistedExecutionLogId, cancellationToken)
                .ConfigureAwait(false)
            : await _executionLogService.GetUnfinishedByRunIdAsync(validRunId, cancellationToken)
                .ConfigureAwait(false);
        if (executionLog == null ||
            await GetOwnedWorkflowAsync(executionLog.WorkflowId, adminUserId).ConfigureAwait(false) == null)
        {
            throw new WorkflowConflictException("运行任务不存在或不属于当前账号。");
        }

        if (executionLog.StartedAt > DateTime.UtcNow.Subtract(PersistedRunRecoveryAge))
        {
            throw new WorkflowConflictException("当前进程未持有该运行；它可能仍由其他实例执行，暂不能安全中止。");
        }

        const string message = "服务重启后未能恢复此运行，已按手动中止结束。";
        executionLog.Complete(false, null, message);
        await _executionLogService.SaveObjectAsync(executionLog).ConfigureAwait(false);
        await _eventPublisher.PublishAsync(
            executionLog.WorkflowId,
            "aborted-after-restart",
            adminUserId,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 汇总属于当前管理员的历史执行日志与进程内实时运行。实时运行由协调器提供节点级进度，
    /// 已完成任务则使用持久化日志，以便应用重启后仍然可查。
    /// </summary>
    public async Task<WorkflowTaskListPage> GetTaskListAsync(
        int adminUserId,
        int? beforeExecutionLogId = null,
        int? workflowId = null,
        string? status = null,
        string? from = null,
        string? to = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var statusFilter = NeuCharWorkflowAnalyticsService.NormalizeStatus(status);
        var fromUtc = NeuCharWorkflowAnalyticsService.ParseDate(from);
        var toUtc = NeuCharWorkflowAnalyticsService.ParseDate(to)?.AddDays(1);
        var workflowNames = await _workflowService.GetNameMapAsync(
            adminUserId,
            cancellationToken).ConfigureAwait(false);
        var workflowIds = workflowNames.Keys.ToList();
        if (workflowId.HasValue && workflowId.Value > 0)
        {
            if (!workflowNames.ContainsKey(workflowId.Value))
            {
                return new WorkflowTaskListPage(Array.Empty<WorkflowTaskListItem>(), false, null);
            }

            workflowIds = new List<int> { workflowId.Value };
        }
        var loadLatest = !beforeExecutionLogId.HasValue || beforeExecutionLogId.Value <= 0;
        var activeRuns = loadLatest
            ? _runCoordinator.GetActiveRuns(adminUserId)
                .Where(run => !workflowId.HasValue || workflowId.Value <= 0 || run.WorkflowId == workflowId.Value)
                .Where(run => !fromUtc.HasValue || run.StartedAt.UtcDateTime >= fromUtc.Value)
                .Where(run => !toUtc.HasValue || run.StartedAt.UtcDateTime < toUtc.Value)
                .ToArray()
            : Array.Empty<NeuCharWorkflowActiveRun>();

        var tasks = string.IsNullOrWhiteSpace(statusFilter) || statusFilter == "running"
            ? activeRuns.Select(run => new WorkflowTaskListItem(
                $"live:{run.RunId:N}",
                run.WorkflowId,
                workflowNames.TryGetValue(run.WorkflowId, out var workflowName) ? workflowName : $"工作流 #{run.WorkflowId}",
                run.Source,
                "running",
                run.StartedAt,
                null,
                BuildLiveSummary(run),
                null,
                run.RunId,
                null,
                false)).ToList()
            : new List<WorkflowTaskListItem>();

        if (workflowIds.Count == 0)
        {
            return new WorkflowTaskListPage(tasks.OrderByDescending(z => z.StartedAt).ToList(), false, null);
        }

        var logPage = await _executionLogService.GetTaskPageAsync(
            workflowIds,
            beforeExecutionLogId,
            TaskListPageSize + 1,
            statusFilter,
            fromUtc,
            toUtc,
            cancellationToken).ConfigureAwait(false);
        var activeRunIds = activeRuns.Select(z => z.RunId).ToHashSet();
        var fetchedLogs = logPage.ToList();
        var pageLogs = fetchedLogs.Take(TaskListPageSize).ToList();
        tasks.AddRange(pageLogs
            // 由协调器托管的运行有节点级实时流；仅隐藏对应 RunId 的数据库镜像，
            // 保留同一 Workflow 的其他未完成任务，避免并发运行时丢任务。
            .Where(log => log.FinishedAt != null ||
                          !activeRunIds.Contains(GetTaskRunId(log.WorkflowId, log.CorrelationId).GetValueOrDefault()))
            .Select(log => new WorkflowTaskListItem(
                $"log:{log.Id}",
                log.WorkflowId,
                string.IsNullOrWhiteSpace(log.WorkflowName)
                    ? (workflowNames.TryGetValue(log.WorkflowId, out var workflowName) ? workflowName : $"工作流 #{log.WorkflowId}")
                    : log.WorkflowName,
                "history",
                log.FinishedAt == null
                    ? "running"
                    : log.Succeeded == true ? "success" : "failed",
                ToUtcOffset(log.StartedAt),
                ToUtcOffset(log.FinishedAt),
                log.ResultSummary,
                log.Succeeded == false ? log.Error : null,
                GetTaskRunId(log.WorkflowId, log.CorrelationId),
                log.Id,
                log.ReplayAvailable)));

        return new WorkflowTaskListPage(
            tasks.OrderByDescending(z => z.StartedAt).ToList(),
            fetchedLogs.Count > TaskListPageSize,
            pageLogs.LastOrDefault()?.Id,
            await _analyticsService.GetSummaryAsync(
                    adminUserId,
                    workflowId,
                    from,
                    to,
                    cancellationToken)
                .ConfigureAwait(false));
    }

    /// <summary>兼容已编译页面与扩展模块的旧任务列表调用签名。</summary>
    public Task<WorkflowTaskListPage> GetTaskListAsync(
        int adminUserId,
        int? beforeExecutionLogId,
        CancellationToken cancellationToken) =>
        GetTaskListAsync(adminUserId, beforeExecutionLogId, null, null, null, null, cancellationToken);

    public Task<WorkflowAnalyticsResult> GetAnalyticsAsync(
        int adminUserId,
        WorkflowAnalyticsQuery query,
        CancellationToken cancellationToken = default) =>
        _analyticsService.GetAsync(adminUserId, query, cancellationToken);

    /// <summary>
    /// 预览当前管理员可清理的运行历史。只处理已完成的执行日志，进程内或数据库中仍未完成的任务不会被包含。
    /// </summary>
    public async Task<WorkflowTaskCleanupPreview> PreviewTaskCleanupAsync(
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow;
        var logs = await GetCompletedTaskLogsAsync(adminUserId, cutoff, cancellationToken).ConfigureAwait(false);
        return ToTaskCleanupPreview(logs, cutoff);
    }

    /// <summary>
    /// 永久删除当前管理员在指定预览截止时间以前已完成的执行日志。运行中任务不在此范围内。
    /// </summary>
    public async Task<WorkflowTaskCleanupResult> CleanupCompletedTasksAsync(
        int adminUserId,
        DateTime cutoff,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var effectiveCutoff = cutoff.Kind == DateTimeKind.Utc ? cutoff : cutoff.ToUniversalTime();
        if (effectiveCutoff == DateTime.MinValue)
        {
            throw new WorkflowInputException("清理请求缺少有效的预览截止时间。");
        }
        if (effectiveCutoff > DateTime.UtcNow)
        {
            effectiveCutoff = DateTime.UtcNow;
        }

        var logs = await GetCompletedTaskLogsAsync(adminUserId, effectiveCutoff, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        // 使用单次 SaveChanges 的批量删除，避免大量历史记录逐条提交导致“快速清理”长时间占用页面。
        await _executionLogService.DeleteAllAsync(logs).ConfigureAwait(false);

        return new WorkflowTaskCleanupResult(logs.Count, effectiveCutoff);
    }

    /// <summary>
    /// 返回已经完成任务的只读运行回看数据。运行中的任务仍由实时协调器展示，避免用不完整事件启动回放。
    /// </summary>
    public async Task<WorkflowRunReplay?> GetReplayAsync(
        int executionLogId,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var executionLog = await _executionLogService.GetObjectAsync(log => log.Id == executionLogId).ConfigureAwait(false);
        if (executionLog == null || await GetOwnedWorkflowAsync(executionLog.WorkflowId, adminUserId).ConfigureAwait(false) == null)
        {
            return null;
        }
        if (executionLog.FinishedAt == null)
        {
            throw new WorkflowConflictException("该工作流仍在运行，请在运行结束后再启动回看。");
        }
        if (!CanReplay(executionLog))
        {
            throw new WorkflowInputException("该任务产生于回看功能启用前，未保存可回放的节点事件或快照。");
        }

        var snapshotLog = !string.IsNullOrWhiteSpace(executionLog.ReplaySnapshotJson)
            ? executionLog
            : await _executionLogService.GetReplaySnapshotAsync(
                executionLog.WorkflowId,
                executionLog.ReplaySnapshotHash!).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(snapshotLog?.ReplaySnapshotJson))
        {
            throw new WorkflowInputException("该任务引用的工作流快照已不可用，无法开始回看。");
        }

        NeuCharWorkflowReplayDefinition definition;
        IReadOnlyList<WorkflowReplayEvent> events;
        try
        {
            definition = JsonSerializer.Deserialize<NeuCharWorkflowReplayDefinition>(
                snapshotLog.ReplaySnapshotJson,
                DesignerJsonOptions) ?? throw new JsonException("快照为空。");
            var progress = JsonSerializer.Deserialize<List<NeuCharWorkflowProgress>>(
                executionLog.ReplayEventsJson!,
                DesignerJsonOptions) ?? new List<NeuCharWorkflowProgress>();
            events = progress.Select((item, index) => new WorkflowReplayEvent(
                    index + 1,
                    item.NodeId,
                    item.NodeName,
                    item.Status,
                    item.Message,
                    item.Output,
                    item.Timestamp,
                    item.OutputSchema,
                    item.Input)
                {
                    ObjectReference = item.ObjectReference
                })
                .ToList();
        }
        catch (JsonException ex)
        {
            throw new WorkflowInputException("该任务的回看数据已损坏，无法解析。", ex);
        }

        return new WorkflowRunReplay(
            executionLog.Id,
            executionLog.WorkflowId,
            executionLog.WorkflowName,
            ToTaskStatus(executionLog),
            ToUtcOffset(executionLog.StartedAt),
            ToUtcOffset(executionLog.FinishedAt),
            executionLog.Succeeded,
            executionLog.ResultSummary,
            executionLog.Error,
            definition,
            events);
    }

    /// <summary>从当时的运行快照创建一个禁用的草稿，避免在回看页直接修改历史或意外触发。</summary>
    public async Task<WorkflowDetail> CopyReplayAsDraftAsync(
        int executionLogId,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        var replay = await GetReplayAsync(executionLogId, adminUserId, cancellationToken).ConfigureAwait(false)
            ?? throw new WorkflowNotFoundException();
        var snapshot = replay.Definition;
        var graph = _workflowEngine.ParseAndValidateGraph(snapshot.GraphJson, requireAllNodesReachable: false);
        var graphJson = JsonSerializer.Serialize(graph, DesignerJsonOptions);
        var copy = new WorkflowEntity(CreateReplayCopyName(snapshot.Name), adminUserId);
        copy.Update(
            copy.Name,
            snapshot.Description,
            graphJson,
            enabled: false,
            triggerType: snapshot.TriggerType,
            triggerConfigJson: snapshot.TriggerConfigJson,
            nextRunAt: null,
            autoSaveMinutes: snapshot.AutoSaveMinutes);
        await _workflowService.SaveObjectAsync(copy).ConfigureAwait(false);
        await SaveVersionAsync(copy, adminUserId, "replay-copy").ConfigureAwait(false);
        await _eventPublisher.PublishAsync(copy.Id, "created-from-replay", adminUserId, cancellationToken).ConfigureAwait(false);
        var editableGraphJson = await _workflowEngine.BuildEditableGraphJsonAsync(copy.GraphJson, cancellationToken)
            .ConfigureAwait(false);
        return ToDetail(copy, editableGraphJson);
    }

    public async Task DeleteAsync(int workflowId, int adminUserId, CancellationToken cancellationToken = default)
    {
        var workflow = await GetOwnedWorkflowAsync(workflowId, adminUserId).ConfigureAwait(false)
            ?? throw new WorkflowNotFoundException();
        var versions = await _workflowVersionService.GetFullListAsync(z => z.WorkflowId == workflow.Id).ConfigureAwait(false);
        foreach (var version in versions)
        {
            await _workflowVersionService.DeleteObjectAsync(version).ConfigureAwait(false);
        }
        var logs = await _executionLogService.GetFullListAsync(z => z.WorkflowId == workflow.Id).ConfigureAwait(false);
        foreach (var log in logs)
        {
            await _executionLogService.DeleteObjectAsync(log).ConfigureAwait(false);
        }
        await SafeDeleteChatMessagesByWorkflowAsync(workflow.Id).ConfigureAwait(false);
        await _workflowService.DeleteObjectAsync(workflow).ConfigureAwait(false);
        await _eventPublisher.PublishAsync(workflow.Id, "deleted", adminUserId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<WorkflowWebhookTriggerResult> TriggerWebhookAsync(
        int workflowId,
        string method,
        string suppliedToken,
        IReadOnlyDictionary<string, object> values,
        CancellationToken cancellationToken = default)
    {
        if (!await IsModuleEnabledAsync().ConfigureAwait(false))
        {
            return WorkflowWebhookTriggerResult.Conflict("NeuChar Workflow 模块未安装或未开启。");
        }
        var workflow = await _workflowService.GetObjectAsync(z => z.Id == workflowId).ConfigureAwait(false);
        if (workflow == null)
        {
            return WorkflowWebhookTriggerResult.NotFound();
        }
        if (!workflow.Enabled || !string.Equals(workflow.TriggerType, "webhook", StringComparison.OrdinalIgnoreCase))
        {
            return WorkflowWebhookTriggerResult.Conflict("工作流未启用 Webhook 触发。");
        }

        NeuCharWorkflowWebhookConfig config;
        try
        {
            config = NeuCharWorkflowWebhookConfig.ParseStored(workflow.TriggerConfigJson);
        }
        catch (InvalidOperationException)
        {
            return WorkflowWebhookTriggerResult.ServerError("Webhook 配置无效，请在 Workflow 页面重新保存。");
        }
        if (!config.IsMethodAllowed(method))
        {
            return WorkflowWebhookTriggerResult.MethodNotAllowed(config.Method);
        }
        if (!TokensEqual(config.Token, suppliedToken))
        {
            return WorkflowWebhookTriggerResult.Unauthorized();
        }

        var selectedValues = config.Parameters.Count == 0
            ? values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)
            : values.Where(pair => config.Parameters.Any(parameter =>
                    string.Equals(parameter.Name, pair.Key, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var missing = config.Parameters
            .Where(parameter => parameter.Required &&
                (!selectedValues.TryGetValue(parameter.Name, out var value) || IsEmpty(value)))
            .Select(parameter => parameter.Name)
            .ToArray();
        if (missing.Length > 0)
        {
            return WorkflowWebhookTriggerResult.BadRequest($"缺少必填 Webhook 参数：{string.Join("、", missing)}。", missing);
        }

        var input = JsonSerializer.Serialize(selectedValues, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (!_runCoordinator.TryStart(workflow.Id, workflow.AdminUserId, input, out var runId, out var error, "webhook"))
        {
            return WorkflowWebhookTriggerResult.Conflict(error);
        }
        return WorkflowWebhookTriggerResult.Accepted(workflow.Id, runId);
    }

    /// <summary>
    /// 打开聊天页面前的引导数据：标题、欢迎语、访客标识与历史消息。
    /// </summary>
    public async Task<WorkflowChatBootstrapResult> GetChatBootstrapAsync(
        int workflowId,
        string participantKey,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidParticipantKey(participantKey))
        {
            return WorkflowChatBootstrapResult.BadRequest("会话标识无效。");
        }
        var access = await GetChatAccessAsync(workflowId, participantKey, cancellationToken).ConfigureAwait(false);
        if (!access.Ok)
        {
            return WorkflowChatBootstrapResult.From(access);
        }
        var workflow = access.Workflow!;
        var config = access.Config!;
        IReadOnlyList<WorkflowChatMessage> messages = _chatSessionService.GetMessages(workflowId, participantKey)
            .Select(z => new WorkflowChatMessage(z.Role, z.Content, z.Timestamp))
            .ToList();
        if (messages.Count == 0)
        {
            // 进程内没有历史（例如 Host 刚重启），从数据库恢复并写回会话。
            messages = await RestoreChatHistoryAsync(workflowId, participantKey, cancellationToken).ConfigureAwait(false);
        }
        var greeting = string.IsNullOrWhiteSpace(config.Greeting)
            ? $"你好！我是「{workflow.Name}」，发送消息即可启动工作流。"
            : config.Greeting;
        return WorkflowChatBootstrapResult.Ok(
            workflowId,
            config.Title ?? workflow.Name,
            greeting,
            access.IsGuest,
            _chatSessionService.HasPendingRun(workflowId, participantKey),
            _chatSessionService.GetPendingRun(workflowId, participantKey),
            messages);
    }

    /// <summary>
    /// 发送一条聊天消息并启动一次工作流运行；同一会话同一时间只允许一个进行中的运行。
    /// </summary>
    public async Task<WorkflowChatSendResult> SendChatMessageAsync(
        int workflowId,
        string participantKey,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidParticipantKey(participantKey))
        {
            return WorkflowChatSendResult.BadRequest("会话标识无效。");
        }
        var normalizedMessage = message?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedMessage))
        {
            return WorkflowChatSendResult.BadRequest("消息内容不能为空。");
        }
        if (normalizedMessage.Length > 8_000)
        {
            return WorkflowChatSendResult.BadRequest("消息内容不能超过 8000 个字符。");
        }
        var access = await GetChatAccessAsync(workflowId, participantKey, cancellationToken).ConfigureAwait(false);
        if (!access.Ok)
        {
            return WorkflowChatSendResult.From(access);
        }
        if (_chatSessionService.HasPendingRun(workflowId, participantKey))
        {
            return WorkflowChatSendResult.Conflict("上一条消息还在处理中，请等待完成后再发送。");
        }
        var workflow = access.Workflow!;
        _chatSessionService.AddMessage(workflowId, participantKey, NeuCharWorkflowChatSessionService.RoleUser, normalizedMessage);
        if (!_runCoordinator.TryStart(workflow.Id, workflow.AdminUserId, normalizedMessage, out var runId, out var error, "chat"))
        {
            return WorkflowChatSendResult.Conflict(error);
        }
        _chatSessionService.SetPendingRun(workflowId, participantKey, runId);
        await SafePersistChatMessageAsync(workflowId, participantKey,
            NeuCharWorkflowChatSessionService.RoleUser, normalizedMessage, runId, cancellationToken).ConfigureAwait(false);
        return WorkflowChatSendResult.Accepted(workflowId, runId);
    }

    /// <summary>
    /// 查询聊天会话关联的运行状态；运行结束后把最终输出或错误写入会话历史。
    /// </summary>
    public async Task<WorkflowChatRunResult> GetChatRunStatusAsync(
        int workflowId,
        string participantKey,
        Guid runId,
        long afterSequence,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidParticipantKey(participantKey))
        {
            return WorkflowChatRunResult.BadRequest("会话标识无效。");
        }
        if (runId == Guid.Empty)
        {
            return WorkflowChatRunResult.BadRequest("运行标识无效。");
        }
        var access = await GetChatAccessAsync(workflowId, participantKey, cancellationToken).ConfigureAwait(false);
        if (!access.Ok)
        {
            return WorkflowChatRunResult.From(access);
        }
        if (!_chatSessionService.HasPendingRunMatching(workflowId, participantKey, runId))
        {
            return WorkflowChatRunResult.NotFound();
        }
        var workflow = access.Workflow!;
        var snapshot = _runCoordinator.GetSnapshot(runId, workflow.AdminUserId, afterSequence);
        if (snapshot == null)
        {
            // 运行可能已被协调器的内存上限回收；释放会话占位，让用户可以重新发送。
            var lostMessage = "运行状态已丢失，请重新发送消息。";
            _chatSessionService.AddMessage(workflowId, participantKey, NeuCharWorkflowChatSessionService.RoleError, lostMessage);
            _chatSessionService.ClearPendingRun(workflowId, participantKey, runId);
            await SafePersistChatMessageAsync(workflowId, participantKey,
                NeuCharWorkflowChatSessionService.RoleError, lostMessage, runId, cancellationToken).ConfigureAwait(false);
            return WorkflowChatRunResult.Failed(workflowId, runId, lostMessage);
        }
        if (snapshot.Running)
        {
            var last = snapshot.Events.LastOrDefault();
            return WorkflowChatRunResult.InProgress(
                workflowId,
                runId,
                last?.Message ?? string.Empty,
                last?.Sequence ?? 0);
        }
        var succeeded = snapshot.Succeeded == true;
        if (succeeded)
        {
            _chatSessionService.AddMessage(
                workflowId,
                participantKey,
                NeuCharWorkflowChatSessionService.RoleAssistant,
                NormalizeChatOutput(snapshot.FinalOutput));
            await SafePersistChatMessageAsync(workflowId, participantKey,
                NeuCharWorkflowChatSessionService.RoleAssistant, NormalizeChatOutput(snapshot.FinalOutput), runId, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            var errorText = string.IsNullOrWhiteSpace(snapshot.ErrorMessage)
                ? "工作流执行失败。"
                : snapshot.ErrorMessage.Trim();
            _chatSessionService.AddMessage(workflowId, participantKey, NeuCharWorkflowChatSessionService.RoleError, errorText);
            await SafePersistChatMessageAsync(workflowId, participantKey,
                NeuCharWorkflowChatSessionService.RoleError, errorText, runId, cancellationToken).ConfigureAwait(false);
        }
        _chatSessionService.ClearPendingRun(workflowId, participantKey, runId);
        return succeeded
            ? WorkflowChatRunResult.Succeeded(workflowId, runId, NormalizeChatOutput(snapshot.FinalOutput))
            : WorkflowChatRunResult.Failed(workflowId, runId, snapshot.ErrorMessage);
    }

    /// <summary>
    /// 清空当前聊天会话（历史消息与进行中的运行关联），工作流运行本身不会被中止。
    /// </summary>
    public async Task<WorkflowChatResult> ResetChatSessionAsync(
        int workflowId,
        string participantKey,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidParticipantKey(participantKey))
        {
            return WorkflowChatResult.BadRequest("会话标识无效。");
        }
        var access = await GetChatAccessAsync(workflowId, participantKey, cancellationToken).ConfigureAwait(false);
        if (!access.Ok)
        {
            return WorkflowChatResult.From(access);
        }
        _chatSessionService.Reset(workflowId, participantKey);
        await SafeDeleteChatMessagesByParticipantAsync(workflowId, participantKey, cancellationToken).ConfigureAwait(false);
        return WorkflowChatResult.Ok();
    }

    /// <summary>
    /// Chat 访问校验：模块、工作流启用状态、触发方式、配置与访客权限。
    /// </summary>
    private async Task<WorkflowChatAccess> GetChatAccessAsync(
        int workflowId,
        string participantKey,
        CancellationToken cancellationToken)
    {
        if (!await IsModuleEnabledAsync().ConfigureAwait(false))
        {
            return WorkflowChatAccess.Conflict("NeuChar Workflow 模块未安装或未开启。");
        }
        var workflow = await _workflowService.GetObjectAsync(z => z.Id == workflowId).ConfigureAwait(false);
        if (workflow == null)
        {
            return WorkflowChatAccess.NotFound();
        }
        if (!workflow.Enabled || !string.Equals(workflow.TriggerType, "chat", StringComparison.OrdinalIgnoreCase))
        {
            return WorkflowChatAccess.Conflict("工作流未启用 Chat 触发。");
        }
        NeuCharWorkflowChatConfig config;
        try
        {
            config = NeuCharWorkflowChatConfig.ParseStored(workflow.TriggerConfigJson);
        }
        catch (InvalidOperationException)
        {
            return WorkflowChatAccess.ServerError("Chat 配置无效，请在 Workflow 页面重新保存。");
        }
        var isGuest = participantKey.StartsWith("guest:", StringComparison.Ordinal);
        if (isGuest && !config.AllowGuest)
        {
            return WorkflowChatAccess.Unauthorized();
        }
        return WorkflowChatAccess.Success(workflow, config, isGuest);
    }

    /// <summary>
    /// Host 重启后从数据库恢复会话历史并写回内存会话；
    /// 持久化失败时降级为空历史，不阻断聊天功能本身。
    /// </summary>
    private async Task<IReadOnlyList<WorkflowChatMessage>> RestoreChatHistoryAsync(
        int workflowId,
        string participantKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var persisted = await _chatMessageService
                .GetHistoryAsync(workflowId, NeuCharWorkflowChatSessionService.HashParticipant(participantKey), cancellationToken)
                .ConfigureAwait(false);
            if (persisted.Count == 0)
            {
                return new List<WorkflowChatMessage>();
            }
            _chatSessionService.SeedMessages(
                workflowId,
                participantKey,
                persisted
                    .Select(z => new WorkflowChatSessionMessage(z.Role, z.Content, z.AddTime.ToUniversalTime()))
                    .ToList());
            return _chatSessionService.GetMessages(workflowId, participantKey)
                .Select(z => new WorkflowChatMessage(z.Role, z.Content, z.Timestamp))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "恢复 NeuChar Workflow Chat 历史失败：WorkflowId={WorkflowId}。", workflowId);
            return new List<WorkflowChatMessage>();
        }
    }

    private async Task SafePersistChatMessageAsync(
        int workflowId,
        string participantKey,
        string role,
        string content,
        Guid runId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _chatMessageService.SaveObjectAsync(
                new NeuCharWorkflowChatMessage(
                    workflowId,
                    NeuCharWorkflowChatSessionService.HashParticipant(participantKey),
                    role,
                    content,
                    runId)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "保存 NeuChar Workflow Chat 消息失败：WorkflowId={WorkflowId}，Role={Role}。", workflowId, role);
        }
    }

    private async Task SafeDeleteChatMessagesByParticipantAsync(
        int workflowId,
        string participantKey,
        CancellationToken cancellationToken)
    {
        try
        {
            await _chatMessageService
                .DeleteByParticipantAsync(workflowId, NeuCharWorkflowChatSessionService.HashParticipant(participantKey), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "清除 NeuChar Workflow Chat 消息失败：WorkflowId={WorkflowId}。", workflowId);
        }
    }

    private async Task SafeDeleteChatMessagesByWorkflowAsync(int workflowId)
    {
        try
        {
            await _chatMessageService.DeleteByWorkflowAsync(workflowId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "删除 NeuChar Workflow Chat 消息失败：WorkflowId={WorkflowId}。", workflowId);
        }
    }

    private static bool IsValidParticipantKey(string? participantKey)
    {
        var normalized = participantKey?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 256)
        {
            return false;
        }
        var index = normalized.IndexOf(':');
        return index is > 1 and < 256
            && (normalized[..index] == "user" || normalized[..index] == "guest");
    }

    private static string NormalizeChatOutput(string? output)
    {
        var normalized = (output ?? string.Empty).Trim();
        if (normalized.Length >= 2 && normalized.StartsWith('"') && normalized.EndsWith('"'))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<string>(normalized);
                if (parsed != null)
                {
                    return parsed;
                }
            }
            catch (JsonException)
            {
                // 不是有效的 JSON 字符串时保留原始文本。
            }
        }
        return normalized.Length == 0 ? "工作流已完成。" : normalized;
    }

    private async Task ValidateWorkflowAsync(WorkflowEntity workflow, CancellationToken cancellationToken)
    {
        NeuCharWorkflowGraph graph;
        try
        {
            graph = _workflowEngine.ParseAndValidateGraph(workflow.GraphJson);
        }
        catch (InvalidOperationException ex)
        {
            throw new WorkflowInputException(ex.Message, ex);
        }
        var validationError = await _workflowEngine.ValidateReferencesAsync(graph, cancellationToken).ConfigureAwait(false);
        if (validationError != null)
        {
            throw new WorkflowInputException(validationError);
        }
        var subWorkflowError = await _workflowEngine.ValidateSubWorkflowReferencesAsync(
            graph,
            workflow.Id,
            workflow.AdminUserId,
            requireEnabled: true,
            cancellationToken).ConfigureAwait(false);
        if (subWorkflowError != null)
        {
            throw new WorkflowInputException(subWorkflowError);
        }
    }

    private async Task EnsureModuleEnabledAsync()
    {
        if (!await IsModuleEnabledAsync().ConfigureAwait(false))
        {
            throw new WorkflowModuleUnavailableException();
        }
    }

    private async Task<bool> IsModuleEnabledAsync()
    {
        var module = await _xncfModuleService.GetObjectAsync(z => z.Uid == new Register().Uid).ConfigureAwait(false);
        return module?.State == XncfModules_State.开放;
    }

    private async Task SaveVersionAsync(WorkflowEntity workflow, int adminUserId, string? saveSource)
    {
        await _workflowVersionService.SaveObjectAsync(new NeuCharWorkflowVersion(workflow, adminUserId, saveSource))
            .ConfigureAwait(false);
        var versions = await _workflowVersionService.GetFullListAsync(
            z => z.WorkflowId == workflow.Id,
            z => z.Revision,
            OrderingType.Descending).ConfigureAwait(false);
        foreach (var obsolete in versions.Skip(5))
        {
            await _workflowVersionService.DeleteObjectAsync(obsolete).ConfigureAwait(false);
        }
    }

    private Task<WorkflowEntity?> GetOwnedWorkflowAsync(int workflowId, int adminUserId) =>
        _workflowService.GetObjectAsync(z => z.Id == workflowId && z.AdminUserId == adminUserId);

    private static IReadOnlyList<AgentWorkflowReference> GetAgentWorkflowReferences(
        NeuCharWorkflowGraph graph)
    {
        return (graph?.Nodes ?? new List<NeuCharWorkflowNode>())
            .Where(node => string.Equals(node.Type, "agent", StringComparison.OrdinalIgnoreCase)
                || string.Equals(node.Type, "agent-group", StringComparison.OrdinalIgnoreCase))
            .Select(node => new
            {
                ProviderId = ReadNodeString(node.Config, "providerId"),
                ObjectId = ReadNodeString(node.Config, "objectId")
            })
            .Where(node => string.Equals(node.ProviderId, "agents-manager", StringComparison.OrdinalIgnoreCase))
            .Select(node =>
            {
                if (node.ObjectId.StartsWith("agent:", StringComparison.OrdinalIgnoreCase))
                {
                    return new AgentWorkflowReference(
                        "agent",
                        ParseNodeId(node.ObjectId, "agent:"));
                }

                if (node.ObjectId.StartsWith("group:", StringComparison.OrdinalIgnoreCase))
                {
                    return new AgentWorkflowReference(
                        "group",
                        ParseNodeId(node.ObjectId, "group:"));
                }

                return null;
            })
            .Where(reference => reference?.Id > 0)
            .Distinct()
            .ToList();
    }

    private static string ReadNodeString(
        System.Text.Json.Nodes.JsonObject? config,
        string key)
        => config?.TryGetPropertyValue(key, out var value) == true
            ? value?.ToString() ?? string.Empty
            : string.Empty;

    private static int ParseNodeId(string value, string prefix)
        => int.TryParse(value[prefix.Length..], out var id) ? id : 0;

    private async Task<List<NeuCharWorkflowExecutionLog>> GetCompletedTaskLogsAsync(
        int adminUserId,
        DateTime cutoff,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workflowIds = (await _workflowService.GetFullListAsync(
            z => z.AdminUserId == adminUserId,
            z => z.Id,
            OrderingType.Ascending).ConfigureAwait(false))
            .Select(z => z.Id)
            .ToList();
        if (workflowIds.Count == 0)
        {
            return new List<NeuCharWorkflowExecutionLog>();
        }

        var logs = await _executionLogService.GetFullListAsync(
            z => workflowIds.Contains(z.WorkflowId) && z.FinishedAt != null && z.FinishedAt <= cutoff,
            z => z.Id,
            OrderingType.Descending).ConfigureAwait(false);
        return logs.ToList();
    }

    private static WorkflowTaskCleanupPreview ToTaskCleanupPreview(
        IReadOnlyCollection<NeuCharWorkflowExecutionLog> logs,
        DateTime cutoff) => new(
        logs.Count,
        logs.Count(log => log.Succeeded == true),
        logs.Count(log => log.Succeeded != true),
        cutoff);

    private static void ValidateSaveCommand(SaveWorkflowCommand? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new WorkflowInputException("工作流名称不能为空。");
        }
        if (request.Name.Trim().Length > 200)
        {
            throw new WorkflowInputException("工作流名称不能超过 200 个字符。");
        }
        if (request.Description?.Length > 10_000 || request.TriggerConfigJson?.Length > 100_000)
        {
            throw new WorkflowInputException("工作流描述或触发器配置超过允许长度。");
        }
        if (request.AutoSaveMinutes is < 0 or > 1440)
        {
            throw new WorkflowInputException("自动保存间隔必须为 0 到 1440 分钟，0 表示关闭。");
        }
    }

    private static void ValidateRunInput(int workflowId, string? input)
    {
        if (workflowId <= 0 || input?.Length > 100_000)
        {
            throw new WorkflowInputException("工作流测试请求无效，输入不能超过 100000 个字符。");
        }
    }

    private static bool IsUnchanged(WorkflowEntity workflow, SaveWorkflowCommand request, string graphJson, bool enabled,
        string triggerType, string triggerConfigJson, int autoSaveMinutes) =>
        string.Equals(workflow.Name, request.Name?.Trim(), StringComparison.Ordinal) &&
        string.Equals(workflow.Description, request.Description?.Trim(), StringComparison.Ordinal) &&
        string.Equals(workflow.GraphJson, graphJson, StringComparison.Ordinal) &&
        workflow.Enabled == enabled &&
        string.Equals(workflow.TriggerType, triggerType, StringComparison.Ordinal) &&
        string.Equals(workflow.TriggerConfigJson, triggerConfigJson, StringComparison.Ordinal) &&
        workflow.AutoSaveMinutes == autoSaveMinutes;

    private async Task<IReadOnlyList<NeuCharWorkflowObservedOutputSchema>> GetObservedOutputSchemasAsync(int workflowId)
    {
        var result = new Dictionary<string, NeuCharWorkflowObservedOutputSchema>(StringComparer.Ordinal);
        var replayEvents = await _executionLogService.GetRecentCompletedReplayEventsAsync(workflowId)
            .ConfigureAwait(false);
        foreach (var replayEventsJson in replayEvents)
        {
            AddObservedSchemas(result, replayEventsJson);
        }
        return result.Values.ToList();
    }

    private static void AddObservedSchemas(
        IDictionary<string, NeuCharWorkflowObservedOutputSchema> result,
        string? replayEventsJson)
    {
        if (string.IsNullOrWhiteSpace(replayEventsJson))
        {
            return;
        }
        try
        {
            var events = JsonSerializer.Deserialize<List<NeuCharWorkflowProgress>>(
                replayEventsJson,
                DesignerJsonOptions) ?? new List<NeuCharWorkflowProgress>();
            foreach (var item in events)
            {
                if (!string.Equals(item.Status, "success", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (string.IsNullOrWhiteSpace(item.OutputSchema))
                {
                    continue;
                }
                var schema = JsonSerializer.Deserialize<NeuCharWorkflowObservedOutputSchema>(
                    item.OutputSchema,
                    DesignerJsonOptions);
                if (schema != null && !result.ContainsKey(schema.NodeId))
                {
                    result[schema.NodeId] = schema;
                }
            }
        }
        catch (JsonException)
        {
            // A historical log must not prevent the workflow editor from opening.
        }
    }

    private static WorkflowDetail ToDetail(
        WorkflowEntity workflow,
        string? graphJson = null,
        bool unchanged = false,
        int runningCount = 0,
        IReadOnlyList<NeuCharWorkflowObservedOutputSchema>? observedOutputSchemas = null) => new(
        workflow.Id, workflow.Name, workflow.Description, graphJson ?? workflow.GraphJson, workflow.Enabled,
        workflow.TriggerType, workflow.TriggerConfigJson, workflow.NextRunAt, workflow.LastRunAt, workflow.LastSucceeded,
        workflow.LastError, workflow.Revision, workflow.AutoSaveMinutes, unchanged, workflow.LastUpdateTime,
        runningCount, observedOutputSchemas);

    private static WorkflowListItem ToListItem(WorkflowEntity workflow) => new(
        workflow.Id, workflow.Name, workflow.Description, workflow.Enabled, workflow.TriggerType, workflow.NextRunAt,
        workflow.LastRunAt, workflow.LastSucceeded, workflow.LastError, workflow.Revision, workflow.AutoSaveMinutes,
        workflow.LastUpdateTime);

    private static string ToTaskStatus(NeuCharWorkflowExecutionLog log) => log.FinishedAt == null
        ? "running"
        : log.Succeeded == true ? "success" : "failed";

    private static bool CanReplay(NeuCharWorkflowExecutionLog log) =>
        log.FinishedAt != null &&
        !string.IsNullOrWhiteSpace(log.ReplaySnapshotHash) &&
        !string.IsNullOrWhiteSpace(log.ReplayEventsJson);

    private static string? BuildLiveSummary(NeuCharWorkflowActiveRun run)
    {
        if (string.IsNullOrWhiteSpace(run.LastNodeName) && string.IsNullOrWhiteSpace(run.LastMessage))
        {
            return "等待工作流引擎开始执行。";
        }

        var node = string.IsNullOrWhiteSpace(run.LastNodeName) ? "工作流" : run.LastNodeName;
        var message = string.IsNullOrWhiteSpace(run.LastMessage) ? "正在执行" : run.LastMessage;
        return $"{node}：{message}";
    }

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static DateTimeOffset? ToUtcOffset(DateTime? value) =>
        value.HasValue ? ToUtcOffset(value.Value) : null;

    private async Task<int> GetRunningCountAsync(
        int adminUserId,
        int workflowId,
        CancellationToken cancellationToken)
    {
        var activeCount = _runCoordinator.GetActiveRunCount(adminUserId, workflowId);
        var persistedCount = await _executionLogService
            .GetUnfinishedCountAsync(workflowId, cancellationToken)
            .ConfigureAwait(false);
        return Math.Max(activeCount, persistedCount);
    }

    private static Guid? GetTaskRunId(int workflowId, string? correlationId)
    {
        var prefix = $"workflow-{workflowId}-run-";
        if (string.IsNullOrWhiteSpace(correlationId) ||
            !correlationId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        return Guid.TryParse(correlationId[prefix.Length..], out var runId) ? runId : null;
    }

    private static string CreateReplayCopyName(string? sourceName)
    {
        const string suffix = "（任务副本）";
        var prefix = string.IsNullOrWhiteSpace(sourceName) ? "Workflow" : sourceName.Trim();
        return prefix.Length + suffix.Length <= 200
            ? prefix + suffix
            : prefix[..(200 - suffix.Length)] + suffix;
    }

    private static bool TokensEqual(string? expected, string? actual)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual))
        {
            return false;
        }
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return expectedBytes.Length == actualBytes.Length && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private static bool IsEmpty(object? value) => value switch
    {
        null => true,
        string text => string.IsNullOrWhiteSpace(text),
        string[] values => values.Length == 0 || values.All(string.IsNullOrWhiteSpace),
        JsonElement element => element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ||
            element.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(element.GetString()),
        _ => false
    };
}

public sealed record SaveWorkflowCommand(
    int Id,
    string? Name,
    string? Description,
    string? GraphJson,
    bool Enabled,
    string? TriggerType,
    string? TriggerConfigJson,
    int AutoSaveMinutes = 3,
    int? ExpectedRevision = null,
    string? SaveSource = null);

public sealed record WorkflowListItem(
    int Id, string Name, string? Description, bool Enabled, string TriggerType, DateTime? NextRunAt,
    DateTime? LastRunAt, bool? LastSucceeded, string? LastError, int Revision, int AutoSaveMinutes, DateTime LastUpdateTime);

public sealed record WorkflowDetail(
    int Id, string Name, string? Description, string GraphJson, bool Enabled, string TriggerType, string TriggerConfigJson,
    DateTime? NextRunAt, DateTime? LastRunAt, bool? LastSucceeded, string? LastError, int Revision, int AutoSaveMinutes,
    bool Unchanged, DateTime LastUpdateTime,
    int RunningCount = 0,
    IReadOnlyList<NeuCharWorkflowObservedOutputSchema>? ObservedOutputSchemas = null);

public sealed record WorkflowTaskListItem(
    string TaskId,
    int WorkflowId,
    string WorkflowName,
    string Source,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    string? Summary,
    string? ErrorMessage,
    Guid? RunId,
    int? ExecutionLogId,
    bool ReplayAvailable);

/// <summary>任务列表的游标分页结果。游标只定位已持久化的执行日志；进行中的任务仅出现在最新页。</summary>
public sealed record WorkflowTaskListPage(
    IReadOnlyList<WorkflowTaskListItem> Items,
    bool HasMore,
    int? NextExecutionLogId,
    WorkflowAnalyticsSummary? Summary = null);

/// <summary>一次快速清理在确认前展示的不可恢复数据范围。</summary>
public sealed record WorkflowTaskCleanupPreview(
    int CompletedCount,
    int SucceededCount,
    int FailedCount,
    DateTime Cutoff);

public sealed record WorkflowTaskCleanupResult(
    int DeletedCount,
    DateTime Cutoff);

public sealed record WorkflowReplayEvent(
    int Sequence,
    string NodeId,
    string NodeName,
    string Status,
    string Message,
    string? Output,
    DateTimeOffset Timestamp,
    string? OutputSchema = null,
    string? Input = null)
{
    public WorkflowObjectExecutionReference? ObjectReference { get; init; }
}

public sealed record WorkflowRunReplay(
    int ExecutionLogId,
    int WorkflowId,
    string WorkflowName,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    bool? Succeeded,
    string? ResultSummary,
    string? ErrorMessage,
    NeuCharWorkflowReplayDefinition Definition,
    IReadOnlyList<WorkflowReplayEvent> Events);

public sealed record WorkflowDesignerFunction(
    string FunctionKey, string Name, string? Description, string ModuleUid, string ModuleName, string ModuleVersion,
    bool ModuleAvailable, string ModuleState, string ParameterSchemaJson, string DefaultParametersJson,
    NeuCharFunctionOutputDescriptor? Output, string? CatalogError)
{
    // 兼容旧 Designer 以 id/functionName 字段定位 Function；稳定标识始终是 moduleUid + functionKey。
    public int Id => 0;
    public string FunctionName => Name;
}

public sealed record WorkflowDesignerData(
    IReadOnlyList<WorkflowDesignerFunction> Functions,
    IReadOnlyList<Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow.WorkflowObjectDescriptor> Objects);

public sealed record WorkflowWebhookTriggerResult(
    int StatusCode,
    string? ErrorMessage = null,
    string? AllowedMethod = null,
    IReadOnlyList<string>? MissingParameters = null,
    int? WorkflowId = null,
    Guid? RunId = null)
{
    public static WorkflowWebhookTriggerResult NotFound() => new(404, "工作流不存在。");
    public static WorkflowWebhookTriggerResult Conflict(string? error) => new(409, error);
    public static WorkflowWebhookTriggerResult ServerError(string? error) => new(500, error);
    public static WorkflowWebhookTriggerResult MethodNotAllowed(string method) => new(405, $"Webhook 只接受 {method.ToUpperInvariant()} 请求。", method);
    public static WorkflowWebhookTriggerResult Unauthorized() => new(401, "Webhook 访问密钥无效。");
    public static WorkflowWebhookTriggerResult BadRequest(string error, IReadOnlyList<string> missing) => new(400, error, MissingParameters: missing);
    public static WorkflowWebhookTriggerResult Accepted(int workflowId, Guid runId) => new(202, WorkflowId: workflowId, RunId: runId);
}

/// <summary>
/// Chat 会话中的一条历史消息。
/// </summary>
public sealed record WorkflowChatMessage(
    string Role,
    string Content,
    DateTimeOffset Timestamp);

/// <summary>
/// Chat 访问校验结果：只有 Ok 时才会携带工作流与配置。
/// </summary>
public sealed record WorkflowChatAccess(
    bool Ok,
    int StatusCode,
    string? ErrorMessage = null,
    WorkflowEntity? Workflow = null,
    NeuCharWorkflowChatConfig? Config = null,
    bool IsGuest = false)
{
    public static WorkflowChatAccess Success(WorkflowEntity workflow, NeuCharWorkflowChatConfig config, bool isGuest)
        => new(true, 200, Workflow: workflow, Config: config, IsGuest: isGuest);
    public static WorkflowChatAccess NotFound() => new(false, 404, "工作流不存在。");
    public static WorkflowChatAccess Conflict(string? error) => new(false, 409, error);
    public static WorkflowChatAccess Unauthorized() => new(false, 401, "该工作流未开启访客访问，请先登录。");
    public static WorkflowChatAccess ServerError(string? error) => new(false, 500, error);
}

/// <summary>
/// 聊天页面引导数据的返回结果。
/// </summary>
public sealed record WorkflowChatBootstrapResult(
    int StatusCode,
    string? ErrorMessage = null,
    int? WorkflowId = null,
    string? Title = null,
    string? Greeting = null,
    bool IsGuest = false,
    bool HasPendingRun = false,
    Guid? PendingRunId = null,
    IReadOnlyList<WorkflowChatMessage>? Messages = null)
{
    public static WorkflowChatBootstrapResult NotFound() => new(404, "工作流不存在。");
    public static WorkflowChatBootstrapResult BadRequest(string error) => new(400, error);
    public static WorkflowChatBootstrapResult From(WorkflowChatAccess access) => new(access.StatusCode, access.ErrorMessage);
    public static WorkflowChatBootstrapResult Ok(
        int workflowId,
        string title,
        string greeting,
        bool isGuest,
        bool hasPendingRun,
        Guid? pendingRunId,
        IReadOnlyList<WorkflowChatMessage> messages)
        => new(200, WorkflowId: workflowId, Title: title, Greeting: greeting, IsGuest: isGuest, HasPendingRun: hasPendingRun, PendingRunId: pendingRunId, Messages: messages);
}

/// <summary>
/// 聊天消息发送结果；202 表示已接受并返回运行标识。
/// </summary>
public sealed record WorkflowChatSendResult(
    int StatusCode,
    string? ErrorMessage = null,
    int? WorkflowId = null,
    Guid? RunId = null)
{
    public static WorkflowChatSendResult NotFound() => new(404, "工作流不存在。");
    public static WorkflowChatSendResult BadRequest(string error) => new(400, error);
    public static WorkflowChatSendResult Conflict(string? error) => new(409, error);
    public static WorkflowChatSendResult From(WorkflowChatAccess access) => new(access.StatusCode, access.ErrorMessage);
    public static WorkflowChatSendResult Accepted(int workflowId, Guid runId) => new(202, WorkflowId: workflowId, RunId: runId);
}

/// <summary>
/// 聊天运行状态查询结果；200 且 Running=false 时，FinalOutput 与 RunError 必有且仅有一个有值。
/// </summary>
public sealed record WorkflowChatRunResult(
    int StatusCode,
    string? ErrorMessage = null,
    int? WorkflowId = null,
    Guid? RunId = null,
    bool Running = false,
    string? FinalOutput = null,
    string? RunError = null,
    string? LastNodeMessage = null,
    long LastSequence = 0)
{
    public static WorkflowChatRunResult NotFound() => new(404, "运行任务不存在或不属于当前会话。");
    public static WorkflowChatRunResult BadRequest(string error) => new(400, error);
    public static WorkflowChatRunResult From(WorkflowChatAccess access) => new(access.StatusCode, access.ErrorMessage);
    public static WorkflowChatRunResult InProgress(int workflowId, Guid runId, string lastNodeMessage, long lastSequence)
        => new(200, WorkflowId: workflowId, RunId: runId, Running: true, LastNodeMessage: lastNodeMessage, LastSequence: lastSequence);
    public static WorkflowChatRunResult Succeeded(int workflowId, Guid runId, string finalOutput)
        => new(200, WorkflowId: workflowId, RunId: runId, FinalOutput: finalOutput);
    public static WorkflowChatRunResult Failed(int workflowId, Guid runId, string? error)
        => new(200, WorkflowId: workflowId, RunId: runId, RunError: error);
}

/// <summary>
/// Chat 会话重置结果。
/// </summary>
public sealed record WorkflowChatResult(
    int StatusCode,
    string? ErrorMessage = null)
{
    public static WorkflowChatResult NotFound() => new(404, "工作流不存在。");
    public static WorkflowChatResult BadRequest(string error) => new(400, error);
    public static WorkflowChatResult From(WorkflowChatAccess access) => new(access.StatusCode, access.ErrorMessage);
    public static WorkflowChatResult Ok() => new(200);
}

public sealed class WorkflowInputException : InvalidOperationException
{
    public WorkflowInputException(string message, Exception? innerException = null) : base(message, innerException) { }
}

public sealed class WorkflowNotFoundException : InvalidOperationException
{
    public WorkflowNotFoundException() : base("工作流不存在或没有访问权限。") { }
}

public sealed class WorkflowConflictException : InvalidOperationException
{
    public WorkflowConflictException(string? message) : base(message) { }
}

public sealed class WorkflowModuleUnavailableException : InvalidOperationException
{
    public WorkflowModuleUnavailableException() : base("NeuChar Workflow 模块未安装或未开启。") { }
}
