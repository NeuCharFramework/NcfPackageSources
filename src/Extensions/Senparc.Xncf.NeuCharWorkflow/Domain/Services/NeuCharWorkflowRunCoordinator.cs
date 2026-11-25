/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowRunCoordinator.cs
    File description: Related implementation.

    Created by: Senparc - 20260809

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20260815
    Change description: v0.2.0 enhance workflow parallelism and execution control

    Modified by: Senparc - 20260817
    Change description: v0.2.0 support pausing and external resumption of Human Input nodes

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance workflow function calls, task control, and replay management

    Modified by: Senparc - 20260909
    Change description: v0.4.0 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.4.0 enhance Chat trigger message persistence and recovery

    Modified by: Senparc - 20261028
    Change description: vNext Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.4.4 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Core.MultiTenant;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowEntity = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow;
using WorkflowExecutionLog = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflowExecutionLog;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>
/// Describes one progress event emitted by a workflow run.
/// </summary>
/// <param name="Sequence">A monotonically increasing sequence number within the run.</param>
/// <param name="NodeId">The identifier of the workflow node that emitted the event.</param>
/// <param name="NodeName">The display name of the workflow node.</param>
/// <param name="Status">The node or execution status associated with the event.</param>
/// <param name="Message">The bounded human-readable progress message.</param>
/// <param name="Output">The bounded output associated with the progress event.</param>
/// <param name="Timestamp">The UTC timestamp at which the event was recorded.</param>
/// <param name="OutputSchema">The optional bounded output schema produced by the node.</param>
public sealed record NeuCharWorkflowRunEvent(
    long Sequence,
    string NodeId,
    string NodeName,
    string Status,
    string Message,
    string Output,
    DateTimeOffset Timestamp,
    string? OutputSchema = null);

/// <summary>
/// Represents the owner-scoped state and incremental progress of a workflow run.
/// </summary>
/// <param name="RunId">The unique identifier of the run.</param>
/// <param name="WorkflowId">The identifier of the workflow being executed.</param>
/// <param name="Running">Indicates whether execution is still in progress.</param>
/// <param name="Succeeded">The completed result, or <see langword="null"/> while unfinished.</param>
/// <param name="ErrorMessage">The bounded failure or cancellation message, when available.</param>
/// <param name="FinalOutput">The bounded final workflow output.</param>
/// <param name="Events">Progress events after the requested sequence boundary.</param>
/// <param name="HumanInteractions">Pending human-interaction requests associated with the run.</param>
/// <param name="RunningCount">The number of active runs for the same owner and workflow.</param>
public sealed record NeuCharWorkflowRunSnapshot(
    Guid RunId,
    int WorkflowId,
    bool Running,
    bool? Succeeded,
    string? ErrorMessage,
    string FinalOutput,
    IReadOnlyList<NeuCharWorkflowRunEvent> Events,
    IReadOnlyList<Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow.WorkflowHumanInteraction>? HumanInteractions = null,
    int RunningCount = 0);

/// <summary>
/// Represents one active workflow execution and its bounded progress history.
/// </summary>
/// <param name="RunId">The unique identifier of the run.</param>
/// <param name="WorkflowId">The identifier of the workflow being executed.</param>
/// <param name="Source">The normalized trigger source used to start the run.</param>
/// <param name="StartedAt">The UTC timestamp at which the run was registered.</param>
/// <param name="LastNodeName">The most recent node name, when progress has been reported.</param>
/// <param name="LastStatus">The most recent node status, when progress has been reported.</param>
/// <param name="LastMessage">The most recent bounded progress message, when available.</param>
/// <param name="LastUpdatedAt">The timestamp of the most recent progress event, when available.</param>
public sealed record NeuCharWorkflowActiveRun(
    Guid RunId,
    int WorkflowId,
    string Source,
    DateTimeOffset StartedAt,
    string? LastNodeName,
    string? LastStatus,
    string? LastMessage,
    DateTimeOffset? LastUpdatedAt);

public sealed class NeuCharWorkflowRunCoordinator
{
    private const int MaxLiveRunEvents = 5_000;

    private sealed class RunState
    {
        private readonly object _gate = new();
        private readonly List<NeuCharWorkflowRunEvent> _events = new();
        private readonly CancellationTokenSource _manualAbort = new();
        private long _sequence;

        /// <summary>
        /// Initializes mutable state for one workflow execution.
        /// </summary>
        /// <param name="runId">The unique identifier assigned to the execution.</param>
        /// <param name="workflowId">The workflow identifier.</param>
        /// <param name="adminUserId">The owner identifier used for access scoping.</param>
        /// <param name="input">The input supplied to the workflow.</param>
        /// <param name="source">The normalized execution source.</param>
        public RunState(Guid runId, int workflowId, int adminUserId, string input, string source)
        {
            RunId = runId;
            WorkflowId = workflowId;
            AdminUserId = adminUserId;
            Input = input;
            Source = source;
            StartedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Creates a run state and associates the optional tenant snapshot with it.
        /// </summary>
        /// <param name="runId">The unique identifier assigned to the execution.</param>
        /// <param name="workflowId">The workflow identifier.</param>
        /// <param name="adminUserId">The owner identifier used for access scoping.</param>
        /// <param name="input">The input supplied to the workflow.</param>
        /// <param name="source">The normalized execution source.</param>
        /// <param name="tenant">The tenant context to restore before execution, if any.</param>
        /// <returns>A new mutable run state.</returns>
        public static RunState Create(
            Guid runId,
            int workflowId,
            int adminUserId,
            string input,
            string source,
            RequestTenantSnapshot? tenant)
        {
            var state = new RunState(runId, workflowId, adminUserId, input, source)
            {
                Tenant = tenant
            };
            return state;
        }

        public Guid RunId { get; }
        public int WorkflowId { get; }
        public int AdminUserId { get; }
        public string Input { get; }
        public string Source { get; }
        public RequestTenantSnapshot? Tenant { get; private set; }
        public DateTimeOffset StartedAt { get; }
        public bool Running { get; private set; } = true;
        public bool? Succeeded { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string? FinalOutput { get; private set; }
        public bool IsManuallyAborted { get; private set; }
        public CancellationToken ManualAbortToken => _manualAbort.Token;

        /// <summary>
        /// Requests cooperative cancellation and records the abort request exactly once.
        /// </summary>
        /// <param name="error">Receives a reason when the run cannot be aborted.</param>
        /// <returns>
        /// <see langword="true"/> when cancellation was accepted or had already been requested;
        /// otherwise, <see langword="false"/> when the run had already completed.
        /// </returns>
        public bool TryAbort(out string? error)
        {
            lock (_gate)
            {
                if (!Running)
                {
                    error = "The Workflow task has ended and cannot be aborted.";
                    return false;
                }
                if (IsManuallyAborted)
                {
                    error = null;
                    return true;
                }

                IsManuallyAborted = true;
                _events.Add(new NeuCharWorkflowRunEvent(
                    ++_sequence,
                    string.Empty,
                    "Workflow",
                    "running",
                    "已请求手动中止，正在等待当前节点响应取消。",
                    string.Empty,
                    DateTimeOffset.UtcNow));
            }
            _manualAbort.Cancel();
            error = null;
            return true;
        }

        /// <summary>
        /// Gets the localized marker used by workflow execution and persistence to detect manual aborts.
        /// </summary>
        /// <returns>The marker text when an abort was requested; otherwise, <see langword="null"/>.</returns>
        public string? GetManualAbortResult()
        {
            lock (_gate)
            {
                return IsManuallyAborted ? "手动中止" : null;
            }
        }

        /// <summary>
        /// Appends one progress event and removes the oldest events beyond the live history limit.
        /// </summary>
        /// <param name="progress">The progress payload to convert into a bounded run event.</param>
        public void Add(NeuCharWorkflowProgress progress)
        {
            lock (_gate)
            {
                _events.Add(new NeuCharWorkflowRunEvent(
                    ++_sequence,
                    progress.NodeId,
                    progress.NodeName,
                    progress.Status,
                    Limit(progress.Message, 4_000),
                    Limit(progress.Output, 20_000),
                    progress.Timestamp,
                    Limit(progress.OutputSchema, 20_000)));
                if (_events.Count > MaxLiveRunEvents)
                {
                    _events.RemoveRange(0, _events.Count - MaxLiveRunEvents);
                }
            }
        }

        /// <summary>
        /// Marks the run as finished and stores bounded completion data.
        /// </summary>
        /// <param name="succeeded">Whether the workflow completed successfully.</param>
        /// <param name="output">The final workflow output, if available.</param>
        /// <param name="error">The failure or cancellation message, if any.</param>
        public void Complete(bool succeeded, string? output, string? error)
        {
            lock (_gate)
            {
                Running = false;
                Succeeded = succeeded;
                FinalOutput = Limit(output, 100_000);
                ErrorMessage = Limit(error, 10_000);
            }
        }

        /// <summary>
        /// Creates an incremental snapshot containing events newer than a sequence boundary.
        /// </summary>
        /// <param name="afterSequence">The last sequence already consumed by the caller.</param>
        /// <returns>A consistent snapshot of the current run state.</returns>
        public NeuCharWorkflowRunSnapshot Snapshot(long afterSequence)
        {
            lock (_gate)
            {
                return new NeuCharWorkflowRunSnapshot(
                    RunId,
                    WorkflowId,
                    Running,
                    Succeeded,
                    ErrorMessage,
                    FinalOutput,
                    _events.Where(z => z.Sequence > afterSequence).ToList());
            }
        }

        /// <summary>
        /// Projects the state into the compact representation used by active-run listings.
        /// </summary>
        /// <returns>The current active-run summary.</returns>
        public NeuCharWorkflowActiveRun ToActiveRun()
        {
            lock (_gate)
            {
                var latest = _events.LastOrDefault();
                return new NeuCharWorkflowActiveRun(
                    RunId,
                    WorkflowId,
                    Source,
                    StartedAt,
                    latest?.NodeName,
                    latest?.Status,
                    latest?.Message,
                    latest?.Timestamp);
            }
        }

        /// <summary>
        /// Bounds a nullable text value so live run state cannot grow without limit.
        /// </summary>
        /// <param name="value">The text to bound.</param>
        /// <param name="maxLength">The maximum number of characters to retain.</param>
        /// <returns>The original value when within the limit, or a truncated value.</returns>
        private static string? Limit(string? value, int maxLength) =>
            string.IsNullOrEmpty(value) || value.Length <= maxLength
                ? value
                : value[..maxLength] + "\n…（输出已截断）";
    }

    private readonly ConcurrentDictionary<Guid, RunState> _runs = new();
    private readonly object _startGate = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<NeuCharWorkflowRunCoordinator> _logger;

    /// <summary>
    /// Initializes the in-process workflow run coordinator.
    /// </summary>
    /// <param name="scopeFactory">Factory used to execute runs in isolated scopes.</param>
    /// <param name="applicationLifetime">Host lifetime used for shutdown cancellation.</param>
    /// <param name="logger">Coordinator logger.</param>
    public NeuCharWorkflowRunCoordinator(
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime applicationLifetime,
        ILogger<NeuCharWorkflowRunCoordinator> logger)
    {
        _scopeFactory = scopeFactory;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
    }

    /// <summary>
    /// Registers and begins a background workflow run.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="adminUserId">The workflow owner identifier.</param>
    /// <param name="input">The workflow input.</param>
    /// <param name="runId">Receives the assigned run identifier.</param>
    /// <param name="error">Receives a scheduling error when startup fails.</param>
    /// <param name="source">The trigger source label.</param>
    /// <param name="tenant">Optional tenant snapshot restored for execution.</param>
    /// <returns><see langword="true"/> when the run is registered.</returns>
    public bool TryStart(
        int workflowId,
        int adminUserId,
        string input,
        out Guid runId,
        out string? error,
        string source = "manual",
        RequestTenantSnapshot? tenant = null)
    {
        lock (_startGate)
        {
            Cleanup();
            runId = Guid.NewGuid();
            var state = RunState.Create(
                runId,
                workflowId,
                adminUserId,
                input ?? string.Empty,
                NormalizeSource(source),
                tenant);
            _runs[runId] = state;
            _ = ExecuteAsync(state);
            error = null;
            return true;
        }
    }

    /// <summary>
    /// Gets an owner-scoped run snapshot containing events after the supplied sequence.
    /// </summary>
    /// <param name="runId">The run identifier.</param>
    /// <param name="adminUserId">The workflow owner identifier.</param>
    /// <param name="afterSequence">The last sequence already consumed.</param>
    /// <returns>The snapshot, or <see langword="null"/> when missing/not owned.</returns>
    public NeuCharWorkflowRunSnapshot? GetSnapshot(Guid runId, int adminUserId, long afterSequence)
    {
        if (!_runs.TryGetValue(runId, out var state) || state.AdminUserId != adminUserId)
        {
            return null;
        }

        return state.Snapshot(Math.Max(0, afterSequence)) with
        {
            RunningCount = GetActiveRunCount(adminUserId, state.WorkflowId)
        };
    }

    /// <summary>
    /// Requests cooperative cancellation for an owner-scoped active run.
    /// </summary>
    /// <param name="runId">The run identifier.</param>
    /// <param name="adminUserId">The workflow owner identifier.</param>
    /// <param name="error">Receives an error when the run cannot be aborted.</param>
    /// <returns><see langword="true"/> when the run exists and abort was requested.</returns>
    public bool TryAbort(Guid runId, int adminUserId, out string? error)
    {
        if (!_runs.TryGetValue(runId, out var state) || state.AdminUserId != adminUserId)
        {
            error = "The run does not exist or does not belong to the current account.";
            return false;
        }
        return state.TryAbort(out error);
    }

    /// <summary>
    /// Returns active runs owned by an administrator.
    /// </summary>
    /// <param name="adminUserId">The workflow owner identifier.</param>
    /// <returns>Active run summaries ordered newest first.</returns>
    public IReadOnlyList<NeuCharWorkflowActiveRun> GetActiveRuns(int adminUserId)
    {
        Cleanup();
        return _runs.Values
            .Where(z => z.AdminUserId == adminUserId && z.Running)
            .Select(z => z.ToActiveRun())
            .OrderByDescending(z => z.StartedAt)
            .ToList();
    }

    /// <summary>
    /// Counts active runs owned by an administrator for one workflow.
    /// </summary>
    /// <param name="adminUserId">The workflow owner identifier.</param>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <returns>The active run count.</returns>
    public int GetActiveRunCount(int adminUserId, int workflowId)
    {
        if (workflowId <= 0)
        {
            return 0;
        }

        Cleanup();
        return _runs.Values.Count(z =>
            z.AdminUserId == adminUserId &&
            z.WorkflowId == workflowId &&
            z.Running);
    }

    /// <summary>
    /// Restores the captured execution scope, validates the workflow, runs it, and persists its outcome.
    /// </summary>
    /// <param name="state">The mutable state describing the scheduled run.</param>
    /// <remarks>
    /// Execution is bounded by application shutdown, manual abort, and a ten-minute timeout.
    /// Exceptions are converted into a failed run state so background execution does not escape to the caller.
    /// </remarks>
    private async Task ExecuteAsync(RunState state)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
            _applicationLifetime.ApplicationStopping,
            state.ManualAbortToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(10));
        try
        {
            using var scope = _scopeFactory.CreateScope();
            // Restore the captured tenant before resolving tenant-aware workflow services.
            state.Tenant?.Restore(scope.ServiceProvider);
            var workflowService = scope.ServiceProvider.GetRequiredService<NeuCharWorkflowService>();
            var executionLogService = scope.ServiceProvider.GetRequiredService<NeuCharWorkflowExecutionLogService>();
            var engine = scope.ServiceProvider.GetRequiredService<NeuCharWorkflowEngine>();
            var workflow = await workflowService.GetObjectAsync(z => z.Id == state.WorkflowId)
                .ConfigureAwait(false);
            if (workflow == null)
            {
                state.Complete(false, null, "工作流不存在或已删除。");
                return;
            }

            if (state.GetManualAbortResult() != null)
            {
                await CompleteManualAbortBeforeExecutionAsync(workflow, workflowService, executionLogService, state)
                    .ConfigureAwait(false);
                return;
            }

            var graph = engine.ParseAndValidateGraph(workflow.GraphJson);
            string? validationError;
            try
            {
                validationError = await engine.ValidateReferencesAsync(graph, timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (state.GetManualAbortResult() != null)
            {
                await CompleteManualAbortBeforeExecutionAsync(workflow, workflowService, executionLogService, state)
                    .ConfigureAwait(false);
                return;
            }
            if (validationError != null)
            {
                state.Complete(false, null, validationError);
                return;
            }
            if (state.GetManualAbortResult() != null)
            {
                await CompleteManualAbortBeforeExecutionAsync(workflow, workflowService, executionLogService, state)
                    .ConfigureAwait(false);
                return;
            }

            var nextRun = string.Equals(state.Source, "interval", StringComparison.Ordinal)
                ? NeuCharWorkflowEngine.CalculateNextRun(workflow.TriggerType, workflow.TriggerConfigJson, DateTime.UtcNow)
                : workflow.NextRunAt;
            workflow.MarkStarted(nextRun);
            await workflowService.SaveRuntimeStartedAsync(workflow).ConfigureAwait(false);
            var result = await engine.RunAsync(
                workflow,
                state.Input,
                timeout.Token,
                state.Add,
                state.RunId.ToString("N"),
                state.GetManualAbortResult).ConfigureAwait(false);
            workflow.MarkCompleted(result.Success, result.ErrorMessage);
            await workflowService.SaveRuntimeCompletedAsync(workflow).ConfigureAwait(false);
            state.Complete(result.Success, result.Output, result.ErrorMessage);
        }
        catch (OperationCanceledException)
        {
            var message = state.GetManualAbortResult() ?? "工作流测试运行已超时或服务正在停止。";
            state.Complete(false, message, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow test run failed: WorkflowId={WorkflowId}, RunId={RunId}", state.WorkflowId, state.RunId);
            state.Complete(false, null, ex.Message);
        }
    }

    /// <summary>
    /// Persists a consistent failed completion when a run is aborted before engine execution begins.
    /// </summary>
    /// <param name="workflow">The workflow entity whose runtime state must be completed.</param>
    /// <param name="workflowService">The service used to persist workflow runtime state.</param>
    /// <param name="executionLogService">The service used to persist the execution log.</param>
    /// <param name="state">The run state receiving the same completion result.</param>
    /// <returns>A task representing the persistence operations.</returns>
    private static async Task CompleteManualAbortBeforeExecutionAsync(
        WorkflowEntity workflow,
        NeuCharWorkflowService workflowService,
        NeuCharWorkflowExecutionLogService executionLogService,
        RunState state)
    {
        const string message = "Manually aborted";
        workflow.MarkCompleted(false, message);
        await workflowService.SaveRuntimeCompletedAsync(workflow).ConfigureAwait(false);

        var executionLog = new WorkflowExecutionLog(
            workflow.Id,
            workflow.Name,
            $"workflow-{workflow.Id}-run-{state.RunId:N}");
        executionLog.Complete(false, message, message, "[]");
        await executionLogService.SaveObjectAsync(executionLog).ConfigureAwait(false);
        state.Complete(false, message, message);
    }

    /// <summary>
    /// Removes completed runs outside the retention window and caps the in-memory run registry.
    /// </summary>
    private void Cleanup()
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(-2);
        foreach (var item in _runs.Where(z => !z.Value.Running && z.Value.StartedAt < cutoff).ToList())
        {
            _runs.TryRemove(item.Key, out _);
        }
        if (_runs.Count <= 100)
        {
            return;
        }
        foreach (var item in _runs.Values
                     .Where(z => !z.Running)
                     .OrderBy(z => z.StartedAt)
                     .Take(_runs.Count - 100))
        {
            _runs.TryRemove(item.RunId, out _);
        }
    }

    /// <summary>
    /// Maps an external trigger label to one of the coordinator's supported source values.
    /// </summary>
    /// <param name="source">The caller-provided trigger label.</param>
    /// <returns>A normalized source value; unknown or empty labels become <c>manual</c>.</returns>
    private static string NormalizeSource(string source) => source?.Trim().ToLowerInvariant() switch
    {
        "webhook" => "webhook",
        "interval" => "interval",
        "chat" => "chat",
        _ => "manual"
    };
}
