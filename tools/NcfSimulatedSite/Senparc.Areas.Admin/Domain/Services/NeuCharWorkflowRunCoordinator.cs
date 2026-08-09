/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowRunCoordinator.cs
    文件功能描述：NeuChar Workflow 测试运行状态、节点进度与 Console 协调器
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

namespace Senparc.Areas.Admin.Domain.Services;

public sealed record NeuCharWorkflowRunEvent(
    long Sequence,
    string NodeId,
    string NodeName,
    string Status,
    string Message,
    string Output,
    DateTimeOffset Timestamp);

public sealed record NeuCharWorkflowRunSnapshot(
    Guid RunId,
    int WorkflowId,
    bool Running,
    bool? Succeeded,
    string ErrorMessage,
    string FinalOutput,
    IReadOnlyList<NeuCharWorkflowRunEvent> Events);

public sealed class NeuCharWorkflowRunCoordinator
{
    private sealed class RunState
    {
        private readonly object _gate = new();
        private readonly List<NeuCharWorkflowRunEvent> _events = new();
        private long _sequence;

        public RunState(Guid runId, int workflowId, int adminUserId, string input)
        {
            RunId = runId;
            WorkflowId = workflowId;
            AdminUserId = adminUserId;
            Input = input;
            StartedAt = DateTimeOffset.UtcNow;
        }

        public Guid RunId { get; }
        public int WorkflowId { get; }
        public int AdminUserId { get; }
        public string Input { get; }
        public DateTimeOffset StartedAt { get; }
        public bool Running { get; private set; } = true;
        public bool? Succeeded { get; private set; }
        public string ErrorMessage { get; private set; }
        public string FinalOutput { get; private set; }

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
                    progress.Timestamp));
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
        out string error)
    {
        lock (_startGate)
        {
            Cleanup();
            if (_runs.Values.Any(z => z.WorkflowId == workflowId && z.Running))
            {
                runId = Guid.Empty;
                error = "当前工作流已有测试运行正在执行。";
                return false;
            }

            runId = Guid.NewGuid();
            var state = new RunState(runId, workflowId, adminUserId, input ?? string.Empty);
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

    private async Task ExecuteAsync(RunState state)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
            _applicationLifetime.ApplicationStopping);
        timeout.CancelAfter(TimeSpan.FromMinutes(10));
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var workflowService = scope.ServiceProvider.GetRequiredService<NeuCharWorkflowService>();
            var engine = scope.ServiceProvider.GetRequiredService<NeuCharWorkflowEngine>();
            var workflow = await workflowService.GetObjectAsync(z => z.Id == state.WorkflowId)
                .ConfigureAwait(false);
            if (workflow == null)
            {
                state.Complete(false, null, "工作流不存在或已删除。");
                return;
            }

            var graph = engine.ParseAndValidateGraph(workflow.GraphJson);
            var validationError = await engine.ValidateReferencesAsync(graph, timeout.Token).ConfigureAwait(false);
            if (validationError != null)
            {
                state.Complete(false, null, validationError);
                return;
            }

            workflow.MarkStarted(workflow.NextRunAt);
            await workflowService.SaveObjectAsync(workflow).ConfigureAwait(false);
            var result = await engine.RunAsync(
                workflow,
                state.Input,
                timeout.Token,
                state.Add).ConfigureAwait(false);
            workflow.MarkCompleted(result.Success, result.ErrorMessage);
            await workflowService.SaveObjectAsync(workflow).ConfigureAwait(false);
            state.Complete(result.Success, result.Output, result.ErrorMessage);
        }
        catch (OperationCanceledException)
        {
            state.Complete(false, null, "工作流测试运行已超时或服务正在停止。");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow 测试运行异常：WorkflowId={WorkflowId}, RunId={RunId}", state.WorkflowId, state.RunId);
            state.Complete(false, null, ex.Message);
        }
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
}
