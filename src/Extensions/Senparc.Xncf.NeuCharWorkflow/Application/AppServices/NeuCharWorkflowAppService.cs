using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharWorkflow.Application.Events;
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
    private static readonly JsonSerializerOptions DesignerJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly NeuCharWorkflowService _workflowService;
    private readonly NeuCharWorkflowVersionService _workflowVersionService;
    private readonly NeuCharWorkflowExecutionLogService _executionLogService;
    private readonly NeuCharWorkflowEngine _workflowEngine;
    private readonly NeuCharWorkflowFunctionService _functionService;
    private readonly NeuCharWorkflowRunCoordinator _runCoordinator;
    private readonly WorkflowEventPublisher _eventPublisher;
    private readonly XncfModuleService _xncfModuleService;

    public NeuCharWorkflowAppService(
        NeuCharWorkflowService workflowService,
        NeuCharWorkflowVersionService workflowVersionService,
        NeuCharWorkflowExecutionLogService executionLogService,
        NeuCharWorkflowEngine workflowEngine,
        NeuCharWorkflowFunctionService functionService,
        NeuCharWorkflowRunCoordinator runCoordinator,
        WorkflowEventPublisher eventPublisher,
        XncfModuleService xncfModuleService)
    {
        _workflowService = workflowService;
        _workflowVersionService = workflowVersionService;
        _executionLogService = executionLogService;
        _workflowEngine = workflowEngine;
        _functionService = functionService;
        _runCoordinator = runCoordinator;
        _eventPublisher = eventPublisher;
        _xncfModuleService = xncfModuleService;
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
        var editableGraphJson = await _workflowEngine.BuildEditableGraphJsonAsync(
            workflow.GraphJson,
            cancellationToken).ConfigureAwait(false);
        var observedSchemas = await GetObservedOutputSchemasAsync(workflow.Id).ConfigureAwait(false);
        return ToDetail(workflow, editableGraphJson, observedOutputSchemas: observedSchemas);
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
            return ToDetail(workflow, editableUnchanged, unchanged: true);
        }

        workflow.Update(request.Name, request.Description, normalizedGraphJson, enabled, triggerType,
            triggerConfigJson, nextRun, autoSaveMinutes);
        await _workflowService.SaveObjectAsync(workflow).ConfigureAwait(false);
        await SaveVersionAsync(workflow, adminUserId, request.SaveSource).ConfigureAwait(false);
        await _eventPublisher.PublishAsync(workflow.Id, "saved", adminUserId, cancellationToken).ConfigureAwait(false);
        var editableGraphJson = await _workflowEngine.BuildEditableGraphJsonAsync(workflow.GraphJson, cancellationToken)
            .ConfigureAwait(false);
        return ToDetail(workflow, editableGraphJson);
    }

    public async Task<NeuCharWorkflowRunResult> RunImmediatelyAsync(int workflowId, int adminUserId, string input,
        CancellationToken cancellationToken = default)
    {
        await EnsureModuleEnabledAsync().ConfigureAwait(false);
        ValidateRunInput(workflowId, input);
        var workflow = await GetOwnedWorkflowAsync(workflowId, adminUserId).ConfigureAwait(false)
            ?? throw new WorkflowNotFoundException();
        await ValidateWorkflowAsync(workflow, cancellationToken).ConfigureAwait(false);
        workflow.MarkStarted(workflow.NextRunAt);
        await _workflowService.SaveObjectAsync(workflow).ConfigureAwait(false);
        var result = await _workflowEngine.RunAsync(workflow, input, cancellationToken).ConfigureAwait(false);
        workflow.MarkCompleted(result.Success, result.ErrorMessage);
        await _workflowService.SaveObjectAsync(workflow).ConfigureAwait(false);
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

    public NeuCharWorkflowRunSnapshot? GetRunStatus(Guid runId, int adminUserId, long afterSequence) =>
        runId == Guid.Empty ? null : _runCoordinator.GetSnapshot(runId, adminUserId, afterSequence);

    public void AbortRun(Guid runId, int adminUserId)
    {
        if (runId == Guid.Empty)
        {
            throw new WorkflowInputException("中止请求缺少有效的运行 ID。");
        }
        if (!_runCoordinator.TryAbort(runId, adminUserId, out var error))
        {
            throw new WorkflowConflictException(error ?? "工作流任务无法中止。");
        }
    }

    /// <summary>
    /// 汇总属于当前管理员的历史执行日志与进程内实时运行。实时运行由协调器提供节点级进度，
    /// 已完成任务则使用持久化日志，以便应用重启后仍然可查。
    /// </summary>
    public async Task<WorkflowTaskListPage> GetTaskListAsync(
        int adminUserId,
        int? beforeExecutionLogId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workflows = await _workflowService.GetFullListAsync(
            z => z.AdminUserId == adminUserId,
            z => z.LastUpdateTime,
            OrderingType.Descending).ConfigureAwait(false);
        var workflowNames = workflows.ToDictionary(z => z.Id, z => z.Name);
        var workflowIds = workflowNames.Keys.ToList();
        var loadLatest = !beforeExecutionLogId.HasValue || beforeExecutionLogId.Value <= 0;
        var activeRuns = loadLatest
            ? _runCoordinator.GetActiveRuns(adminUserId)
            : Array.Empty<NeuCharWorkflowActiveRun>();

        var tasks = activeRuns.Select(run => new WorkflowTaskListItem(
            $"live:{run.RunId:N}",
            run.WorkflowId,
            workflowNames.TryGetValue(run.WorkflowId, out var workflowName) ? workflowName : $"工作流 #{run.WorkflowId}",
            run.Source,
            "running",
            run.StartedAt.UtcDateTime,
            null,
            BuildLiveSummary(run),
            null,
            run.RunId,
            null,
            false)).ToList();

        if (workflowIds.Count == 0)
        {
            return new WorkflowTaskListPage(tasks.OrderByDescending(z => z.StartedAt).ToList(), false, null);
        }

        var logPage = await _executionLogService.GetObjectListAsync(
            1,
            TaskListPageSize + 1,
            z => workflowIds.Contains(z.WorkflowId) &&
                 (!beforeExecutionLogId.HasValue || beforeExecutionLogId.Value <= 0 || z.Id < beforeExecutionLogId.Value),
            z => z.Id,
            OrderingType.Descending).ConfigureAwait(false);
        var activeWorkflowIds = activeRuns.Select(z => z.WorkflowId).ToHashSet();
        var fetchedLogs = logPage.ToList();
        var pageLogs = fetchedLogs.Take(TaskListPageSize).ToList();
        tasks.AddRange(pageLogs
            // 由协调器托管的运行有节点级实时流；隐藏其尚未完成的数据库镜像，避免同一任务显示两次。
            .Where(log => log.FinishedAt != null || !activeWorkflowIds.Contains(log.WorkflowId))
            .Select(log => new WorkflowTaskListItem(
                $"log:{log.Id}",
                log.WorkflowId,
                string.IsNullOrWhiteSpace(log.WorkflowName)
                    ? (workflowNames.TryGetValue(log.WorkflowId, out var workflowName) ? workflowName : $"工作流 #{log.WorkflowId}")
                    : log.WorkflowName,
                "history",
                ToTaskStatus(log),
                log.StartedAt,
                log.FinishedAt,
                log.ResultSummary,
                log.Succeeded == false ? log.Error : null,
                GetTaskRunId(log.WorkflowId, log.CorrelationId),
                log.Id,
                CanReplay(log))));

        return new WorkflowTaskListPage(
            tasks.OrderByDescending(z => z.StartedAt).ToList(),
            fetchedLogs.Count > TaskListPageSize,
            pageLogs.LastOrDefault()?.Id);
    }

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
                item.Input)).ToList();
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
            executionLog.StartedAt,
            executionLog.FinishedAt,
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
        var logs = await _executionLogService.GetRecentCompletedAsync(workflowId).ConfigureAwait(false);
        foreach (var log in logs)
        {
            AddObservedSchemas(result, log.ReplayEventsJson);
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
        IReadOnlyList<NeuCharWorkflowObservedOutputSchema>? observedOutputSchemas = null) => new(
        workflow.Id, workflow.Name, workflow.Description, graphJson ?? workflow.GraphJson, workflow.Enabled,
        workflow.TriggerType, workflow.TriggerConfigJson, workflow.NextRunAt, workflow.LastRunAt, workflow.LastSucceeded,
        workflow.LastError, workflow.Revision, workflow.AutoSaveMinutes, unchanged, workflow.LastUpdateTime,
        observedOutputSchemas);

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
    IReadOnlyList<NeuCharWorkflowObservedOutputSchema>? ObservedOutputSchemas = null);

public sealed record WorkflowTaskListItem(
    string TaskId,
    int WorkflowId,
    string WorkflowName,
    string Source,
    string Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string? Summary,
    string? ErrorMessage,
    Guid? RunId,
    int? ExecutionLogId,
    bool ReplayAvailable);

/// <summary>任务列表的游标分页结果。游标只定位已持久化的执行日志；进行中的任务仅出现在最新页。</summary>
public sealed record WorkflowTaskListPage(
    IReadOnlyList<WorkflowTaskListItem> Items,
    bool HasMore,
    int? NextExecutionLogId);

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
    string? Input = null);

public sealed record WorkflowRunReplay(
    int ExecutionLogId,
    int WorkflowId,
    string WorkflowName,
    string Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
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
