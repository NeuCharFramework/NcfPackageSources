/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowRunCoordinator.cs
    文件功能描述：NeuChar Workflow 测试运行状态、节点进度与 Console 协调器


    创建标识：Senparc - 20260809

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowEntity = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow;
using WorkflowExecutionLog = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflowExecutionLog;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

public sealed record NeuCharWorkflowRunEvent(
    long Sequence,
    string NodeId,
    string NodeName,
    string Status,
    string Message,
    string Output,
    DateTimeOffset Timestamp,
    string? OutputSchema = null);

public sealed record NeuCharWorkflowRunSnapshot(
    Guid RunId,
    int WorkflowId,
    bool Running,
    bool? Succeeded,
    string ErrorMessage,
    string FinalOutput,
    IReadOnlyList<NeuCharWorkflowRunEvent> Events);

/// <summary>
/// 供任务列表使用的轻量实时运行状态。不返回原始输入和完整输出，避免在列表页意外暴露敏感数据。
/// </summary>
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
    private sealed class RunState
    {
        private readonly object _gate = new();
        private readonly List<NeuCharWorkflowRunEvent> _events = new();
        private readonly CancellationTokenSource _manualAbort = new();
        private long _sequence;

        public RunState(Guid runId, int workflowId, int adminUserId, string input, string source)
        {
            RunId = runId;
            WorkflowId = workflowId;
            AdminUserId = adminUserId;
            Input = input;
            Source = source;
            StartedAt = DateTimeOffset.UtcNow;
        }

        public Guid RunId { get; }
        public int WorkflowId { get; }
        public int AdminUserId { get; }
        public string Input { get; }
        public string Source { get; }
        public DateTimeOffset StartedAt { get; }
        public bool Running { get; private set; } = true;
        public bool? Succeeded { get; private set; }
        public string ErrorMessage { get; private set; }
        public string FinalOutput { get; private set; }
        public bool IsManuallyAborted { get; private set; }
        public CancellationToken ManualAbortToken => _manualAbort.Token;

        public bool TryAbort(out string? error)
        {
            lock (_gate)
            {
                if (!Running)
                {
                    error = "该工作流任务已结束，无法中止。";
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

        public string? GetManualAbortResult()
        {
            lock (_gate)
            {
                return IsManuallyAborted ? "手动中止" : null;
            }
        }

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
                if (_events.Count > 500)
                {
                    _events.RemoveRange(0, _events.Count - 500);
                }
            }
        }

        public void Complete(bool succeeded, string output, string error)
        {
            lock (_gate)
            {
                Running = false;
                Succeeded = succeeded;
                FinalOutput = Limit(output, 100_000);
                ErrorMessage = Limit(error, 10_000);
            }
        }

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

        private static string Limit(string value, int maxLength) =>
            string.IsNullOrEmpty(value) || value.Length <= maxLength
                ? value
                : value[..maxLength] + "\n…（输出已截断）";
    }

    private readonly ConcurrentDictionary<Guid, RunState> _runs = new();
    private readonly object _startGate = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<NeuCharWorkflowRunCoordinator> _logger;

    public NeuCharWorkflowRunCoordinator(
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime applicationLifetime,
        ILogger<NeuCharWorkflowRunCoordinator> logger)
    {
        _scopeFactory = scopeFactory;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
    }

    public bool TryStart(
        int workflowId,
        int adminUserId,
        string input,
        out Guid runId,
        out string error,
        string source = "manual")
    {
        lock (_startGate)
        {
            Cleanup();
            if (_runs.Values.Any(z => z.WorkflowId == workflowId && z.Running))
            {
                runId = Guid.Empty;
                error = "当前工作流已有运行正在执行。";
                return false;
            }

            runId = Guid.NewGuid();
            var state = new RunState(runId, workflowId, adminUserId, input ?? string.Empty, NormalizeSource(source));
            _runs[runId] = state;
            _ = ExecuteAsync(state);
            error = null;
            return true;
        }
    }

    public NeuCharWorkflowRunSnapshot GetSnapshot(Guid runId, int adminUserId, long afterSequence)
    {
        return _runs.TryGetValue(runId, out var state) && state.AdminUserId == adminUserId
            ? state.Snapshot(Math.Max(0, afterSequence))
            : null;
    }

    public bool TryAbort(Guid runId, int adminUserId, out string? error)
    {
        if (!_runs.TryGetValue(runId, out var state) || state.AdminUserId != adminUserId)
        {
            error = "运行任务不存在或不属于当前账号。";
            return false;
        }
        return state.TryAbort(out error);
    }

    public IReadOnlyList<NeuCharWorkflowActiveRun> GetActiveRuns(int adminUserId)
    {
        Cleanup();
        return _runs.Values
            .Where(z => z.AdminUserId == adminUserId && z.Running)
            .Select(z => z.ToActiveRun())
            .OrderByDescending(z => z.StartedAt)
            .ToList();
    }

    private async Task ExecuteAsync(RunState state)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
            _applicationLifetime.ApplicationStopping,
            state.ManualAbortToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(10));
        try
        {
            using var scope = _scopeFactory.CreateScope();
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
            _logger.LogError(ex, "Workflow 测试运行异常：WorkflowId={WorkflowId}, RunId={RunId}", state.WorkflowId, state.RunId);
            state.Complete(false, null, ex.Message);
        }
    }

    private static async Task CompleteManualAbortBeforeExecutionAsync(
        WorkflowEntity workflow,
        NeuCharWorkflowService workflowService,
        NeuCharWorkflowExecutionLogService executionLogService,
        RunState state)
    {
        const string message = "手动中止";
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

    private static string NormalizeSource(string source) => source?.Trim().ToLowerInvariant() switch
    {
        "webhook" => "webhook",
        "interval" => "interval",
        _ => "manual"
    };
}
