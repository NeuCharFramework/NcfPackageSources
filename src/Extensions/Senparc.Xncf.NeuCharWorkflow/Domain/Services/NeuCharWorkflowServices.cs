/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowServices.cs
    File description: domain services and business-process implementation
    -

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20260815
    Change description: v0.2.0 enhance workflow parallelism and execution control

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance workflow function calls, task control, and replay management

    Modified by: Senparc - 20260829
    Change description: v0.3.0 add workflow analysis queries and admin-side visualization

    Modified by: Senparc - 20260913
    Change description: v0.4.0 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.4.0 enhance Chat trigger message persistence and recovery

    Modified by: Senparc - 20261001
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.4.4 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharWorkflow.ACL;
using Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel;
using WorkflowEntity = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

public sealed class NeuCharWorkflowService : WorkflowClientServiceBase<WorkflowEntity>
{
    public NeuCharWorkflowService(INeuCharWorkflowRepository repository, IServiceProvider serviceProvider)
        : base(repository, serviceProvider) { }

    /// <summary>
    /// Persists runtime-start fields for a workflow without rewriting the full graph.
    /// </summary>
    /// <param name="workflow">The workflow whose runtime fields were updated.</param>
    public Task SaveRuntimeStartedAsync(WorkflowEntity workflow) =>
        SaveRuntimePropertiesAsync(workflow,
            nameof(WorkflowEntity.LastRunAt),
            nameof(WorkflowEntity.NextRunAt),
            nameof(WorkflowEntity.LastUpdateTime));

    /// <summary>
    /// Persists runtime completion fields for a workflow without rewriting the full graph.
    /// </summary>
    /// <param name="workflow">The workflow whose completion fields were updated.</param>
    public Task SaveRuntimeCompletedAsync(WorkflowEntity workflow) =>
        SaveRuntimePropertiesAsync(workflow,
            nameof(WorkflowEntity.LastSucceeded),
            nameof(WorkflowEntity.LastError),
            nameof(WorkflowEntity.LastUpdateTime));

    /// <summary>
    /// Returns the administrator-owned workflow ID/name map ordered by identifier.
    /// </summary>
    /// <param name="adminUserId">The workflow owner identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>A workflow ID/name map.</returns>
    public async Task<IReadOnlyDictionary<int, string>> GetNameMapAsync(
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        return await RepositoryBase.GeAll(z => z.Id, OrderingType.Ascending)
            .AsNoTracking()
            .Where(z => z.AdminUserId == adminUserId)
            .Select(z => new { z.Id, z.Name })
            .ToDictionaryAsync(z => z.Id, z => z.Name, cancellationToken)
            .ConfigureAwait(false);
    }

    private Task SaveRuntimePropertiesAsync(WorkflowEntity workflow, params string[] propertyNames)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        return RepositoryBase.SavePropertiesAsync(workflow, propertyNames);
    }
}

public sealed class NeuCharWorkflowVersionService : WorkflowClientServiceBase<NeuCharWorkflowVersion>
{
    public NeuCharWorkflowVersionService(INeuCharWorkflowVersionRepository repository, IServiceProvider serviceProvider)
        : base(repository, serviceProvider) { }
}

public sealed class NeuCharWorkflowExecutionLogService : WorkflowClientServiceBase<NeuCharWorkflowExecutionLog>
{
    public NeuCharWorkflowExecutionLogService(INeuCharWorkflowExecutionLogRepository repository, IServiceProvider serviceProvider)
        : base(repository, serviceProvider) { }

    /// <summary>
    /// Retrieves the newest execution log containing a replay snapshot.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <returns>The latest replayable log, or <see langword="null"/>.</returns>
    public async Task<NeuCharWorkflowExecutionLog?> GetLatestReplaySnapshotAsync(int workflowId)
    {
        var logs = await GetFullListAsync(
            log => log.WorkflowId == workflowId,
            log => log.StartedAt,
            OrderingType.Descending).ConfigureAwait(false);
        return logs.FirstOrDefault(log => !string.IsNullOrWhiteSpace(log.ReplaySnapshotJson));
    }

    /// <summary>
    /// Retrieves up to twenty recent successful execution logs for a workflow.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <returns>Recent completed execution logs.</returns>
    public async Task<IReadOnlyList<NeuCharWorkflowExecutionLog>> GetRecentCompletedAsync(int workflowId)
    {
        var logs = await GetObjectListAsync(
            1,
            20,
            log => log.WorkflowId == workflowId && log.Succeeded == true,
            log => log.StartedAt,
            OrderingType.Descending).ConfigureAwait(false);
        return logs.ToList();
    }

    /// <summary>
    /// Retrieves serialized replay-event payloads from recent successful workflow executions.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="maxLogs">Maximum number of logs to inspect.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>Serialized replay-event lists.</returns>
    public async Task<IReadOnlyList<string>> GetRecentCompletedReplayEventsAsync(
        int workflowId,
        int maxLogs = 5,
        CancellationToken cancellationToken = default)
    {
        if (workflowId <= 0 || maxLogs <= 0)
        {
            return Array.Empty<string>();
        }

        return await RepositoryBase.GeAll(z => z.StartedAt, OrderingType.Descending)
            .AsNoTracking()
            .Where(z => z.WorkflowId == workflowId &&
                        z.Succeeded == true &&
                        z.ReplayEventsJson != null &&
                        z.ReplayEventsJson != string.Empty)
            .OrderByDescending(z => z.StartedAt)
            .Take(maxLogs)
            .Select(z => z.ReplayEventsJson!)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves a paged, filtered execution-log summary for workflow task lists.
    /// </summary>
    /// <param name="workflowIds">Workflow identifiers visible to the caller.</param>
    /// <param name="beforeExecutionLogId">Optional descending pagination cursor.</param>
    /// <param name="pageSize">Maximum number of summaries.</param>
    /// <param name="statusFilter">Optional running/success/failed filter.</param>
    /// <param name="fromUtc">Optional inclusive start time.</param>
    /// <param name="toUtc">Optional exclusive end time.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>Task log summaries ordered newest first.</returns>
    public async Task<IReadOnlyList<NeuCharWorkflowTaskLogSummary>> GetTaskPageAsync(
        IReadOnlyCollection<int> workflowIds,
        int? beforeExecutionLogId,
        int pageSize,
        string? statusFilter = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (workflowIds == null || workflowIds.Count == 0 || pageSize <= 0)
        {
            return Array.Empty<NeuCharWorkflowTaskLogSummary>();
        }

        var ids = workflowIds.Distinct().ToList();
        var query = RepositoryBase.GeAll(z => z.Id, OrderingType.Descending)
            .AsNoTracking()
            .Where(z => ids.Contains(z.WorkflowId));
        if (fromUtc.HasValue)
        {
            query = query.Where(z => z.StartedAt >= fromUtc.Value);
        }
        if (toUtc.HasValue)
        {
            query = query.Where(z => z.StartedAt < toUtc.Value);
        }
        if (string.Equals(statusFilter, "running", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(z => z.FinishedAt == null);
        }
        else if (string.Equals(statusFilter, "success", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(z => z.FinishedAt != null && z.Succeeded == true);
        }
        else if (string.Equals(statusFilter, "failed", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(z => z.FinishedAt != null && z.Succeeded != true);
        }
        if (beforeExecutionLogId.HasValue && beforeExecutionLogId.Value > 0)
        {
            query = query.Where(z => z.Id < beforeExecutionLogId.Value);
        }

        return await query
            .OrderByDescending(z => z.Id)
            .Take(pageSize)
            .Select(z => new NeuCharWorkflowTaskLogSummary(
                z.Id,
                z.WorkflowId,
                z.WorkflowName,
                z.CorrelationId,
                z.StartedAt,
                z.FinishedAt,
                z.Succeeded,
                z.ResultSummary,
                z.Error,
                z.ReplaySnapshotHash != null &&
                z.ReplayEventsJson != null))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves lightweight task-summary rows for workflow analytics.
    /// </summary>
    /// <param name="workflowIds">Workflow identifiers visible to the caller.</param>
    /// <param name="fromUtc">Optional inclusive start time.</param>
    /// <param name="toUtc">Optional exclusive end time.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>Task summary source rows.</returns>
    public async Task<IReadOnlyList<NeuCharWorkflowTaskSummarySource>> GetTaskSummaryAsync(
        IReadOnlyCollection<int> workflowIds,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (workflowIds == null || workflowIds.Count == 0)
        {
            return Array.Empty<NeuCharWorkflowTaskSummarySource>();
        }

        var ids = workflowIds.Distinct().ToList();
        var query = RepositoryBase.GeAll(z => z.Id, OrderingType.Ascending)
            .AsNoTracking()
            .Where(z => ids.Contains(z.WorkflowId));
        if (fromUtc.HasValue)
        {
            query = query.Where(z => z.StartedAt >= fromUtc.Value);
        }
        if (toUtc.HasValue)
        {
            query = query.Where(z => z.StartedAt < toUtc.Value);
        }

        return await query
            .Select(z => new NeuCharWorkflowTaskSummarySource(
                z.Id,
                z.WorkflowId,
                z.CorrelationId,
                z.StartedAt,
                z.FinishedAt,
                z.Succeeded))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the fields needed by analytics, excluding large replay snapshots.
    /// </summary>
    /// <param name="workflowIds">Workflow identifiers visible to the caller.</param>
    /// <param name="statusFilter">Optional running/success/failed filter.</param>
    /// <param name="fromUtc">Optional inclusive start time.</param>
    /// <param name="toUtc">Optional exclusive end time.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>Analytics log rows.</returns>
    public async Task<IReadOnlyList<NeuCharWorkflowAnalyticsLog>> GetAnalyticsLogsAsync(
        IReadOnlyCollection<int> workflowIds,
        string? statusFilter = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (workflowIds == null || workflowIds.Count == 0)
        {
            return Array.Empty<NeuCharWorkflowAnalyticsLog>();
        }

        var ids = workflowIds.Distinct().ToList();
        var query = RepositoryBase.GeAll(z => z.StartedAt, OrderingType.Descending)
            .AsNoTracking()
            .Where(z => ids.Contains(z.WorkflowId));
        if (fromUtc.HasValue)
        {
            query = query.Where(z => z.StartedAt >= fromUtc.Value);
        }
        if (toUtc.HasValue)
        {
            query = query.Where(z => z.StartedAt < toUtc.Value);
        }
        if (string.Equals(statusFilter, "running", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(z => z.FinishedAt == null);
        }
        else if (string.Equals(statusFilter, "success", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(z => z.FinishedAt != null && z.Succeeded == true);
        }
        else if (string.Equals(statusFilter, "failed", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(z => z.FinishedAt != null && z.Succeeded != true);
        }

        // Keep only fields needed by analytics. ReplaySnapshotJson is intentionally excluded.
        return await query
            .OrderByDescending(z => z.StartedAt)
            .Select(z => new NeuCharWorkflowAnalyticsLog(
                z.Id,
                z.WorkflowId,
                z.WorkflowName,
                z.CorrelationId,
                z.StartedAt,
                z.FinishedAt,
                z.Succeeded,
                z.ResultSummary,
                z.Error,
                z.ReplaySnapshotHash,
                z.ReplayEventsJson))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<int> GetUnfinishedCountAsync(
        int workflowId,
        CancellationToken cancellationToken = default)
    {
        if (workflowId <= 0)
        {
            return Task.FromResult(0);
        }

        return RepositoryBase.GeAll(z => z.Id, OrderingType.Ascending)
            .AsNoTracking()
            .CountAsync(
                z => z.WorkflowId == workflowId && z.FinishedAt == null,
                cancellationToken);
    }

    public async Task<NeuCharWorkflowExecutionLog?> GetUnfinishedByRunIdAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        if (runId == Guid.Empty)
        {
            return null;
        }

        var suffix = $"-run-{runId:N}";
        return await RepositoryBase.GeAll(z => z.Id, OrderingType.Descending)
            .Where(z => z.FinishedAt == null && z.CorrelationId.EndsWith(suffix))
            .OrderByDescending(z => z.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<NeuCharWorkflowExecutionLog?> GetUnfinishedByIdAsync(
        int executionLogId,
        CancellationToken cancellationToken = default)
    {
        if (executionLogId <= 0)
        {
            return Task.FromResult<NeuCharWorkflowExecutionLog?>(null);
        }

        return RepositoryBase.GeAll(z => z.Id, OrderingType.Ascending)
            .FirstOrDefaultAsync(
                z => z.Id == executionLogId && z.FinishedAt == null,
                cancellationToken);
    }

    public async Task<NeuCharWorkflowExecutionLog?> GetReplaySnapshotAsync(int workflowId, string snapshotHash)
    {
        if (string.IsNullOrWhiteSpace(snapshotHash))
        {
            return null;
        }

        var logs = await GetFullListAsync(
            log => log.WorkflowId == workflowId,
            log => log.StartedAt,
            OrderingType.Descending).ConfigureAwait(false);
        return logs.FirstOrDefault(log =>
            string.Equals(log.ReplaySnapshotHash, snapshotHash, StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(log.ReplaySnapshotJson));
    }
}

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public sealed class NeuCharWorkflowChatMessageService : WorkflowClientServiceBase<NeuCharWorkflowChatMessage>
{
    ///Legacy comment translated to English.
    public const int RetentionDays = 30;
    ///Legacy comment translated to English.
    public const int MaxHistoryCount = 200;

    public NeuCharWorkflowChatMessageService(
        INeuCharWorkflowChatMessageRepository repository,
        IServiceProvider serviceProvider)
        : base(repository, serviceProvider) { }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public async Task<IReadOnlyList<NeuCharWorkflowChatMessage>> GetHistoryAsync(
        int workflowId,
        string participantKeyHash,
        CancellationToken cancellationToken = default)
    {
        if (workflowId <= 0 || string.IsNullOrWhiteSpace(participantKeyHash))
        {
            return Array.Empty<NeuCharWorkflowChatMessage>();
        }
        var messages = await GetObjectListAsync(
            1,
            MaxHistoryCount,
            z => z.WorkflowId == workflowId && z.ParticipantKeyHash == participantKeyHash,
            z => z.Id,
            OrderingType.Descending).ConfigureAwait(false);
        return messages
            .OrderBy(z => z.Id)
            .Take(MaxHistoryCount)
            .ToList();
    }

    ///Legacy comment translated to English.
    public async Task DeleteByParticipantAsync(int workflowId, string participantKeyHash, CancellationToken cancellationToken = default)
    {
        if (workflowId <= 0 || string.IsNullOrWhiteSpace(participantKeyHash))
        {
            return;
        }
        var messages = await GetFullListAsync(
            z => z.WorkflowId == workflowId && z.ParticipantKeyHash == participantKeyHash,
            z => z.Id,
            OrderingType.Ascending).ConfigureAwait(false);
        foreach (var message in messages)
        {
            await DeleteObjectAsync(message).ConfigureAwait(false);
        }
    }

    ///Legacy comment translated to English.
    public async Task DeleteByWorkflowAsync(int workflowId, CancellationToken cancellationToken = default)
    {
        if (workflowId <= 0)
        {
            return;
        }
        var messages = await GetFullListAsync(
            z => z.WorkflowId == workflowId,
            z => z.Id,
            OrderingType.Ascending).ConfigureAwait(false);
        foreach (var message in messages)
        {
            await DeleteObjectAsync(message).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public async Task<int> DeleteExpiredAsync(DateTime cutoff, int cap = 1000, CancellationToken cancellationToken = default)
    {
        var expired = await GetFullListAsync(
            z => z.AddTime < cutoff,
            z => z.Id,
            OrderingType.Ascending).ConfigureAwait(false);
        var count = 0;
        foreach (var message in expired.Take(cap))
        {
            await DeleteObjectAsync(message).ConfigureAwait(false);
            count++;
        }
        return count;
    }
}

public sealed record NeuCharWorkflowTaskLogSummary(
    int Id,
    int WorkflowId,
    string WorkflowName,
    string CorrelationId,
    DateTime StartedAt,
    DateTime? FinishedAt,
    bool? Succeeded,
    string? ResultSummary,
    string? Error,
    bool ReplayAvailable);

public sealed record NeuCharWorkflowTaskSummarySource(
    int Id,
    int WorkflowId,
    string CorrelationId,
    DateTime StartedAt,
    DateTime? FinishedAt,
    bool? Succeeded);

public sealed record NeuCharWorkflowAnalyticsLog(
    int Id,
    int WorkflowId,
    string WorkflowName,
    string CorrelationId,
    DateTime StartedAt,
    DateTime? FinishedAt,
    bool? Succeeded,
    string? ResultSummary,
    string? Error,
    string? ReplaySnapshotHash,
    string? ReplayEventsJson);
