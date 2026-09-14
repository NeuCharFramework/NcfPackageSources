/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NeuCharWorkflowServices.cs
    文件功能描述：领域服务与业务流程实现


    创建标识：Senparc - 20260810

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

    修改标识：Senparc - 20260815
    修改描述：v0.2.0 增强工作流并行与运行控制

    修改标识：Senparc - 20260822
    修改描述：v0.2.0 增强工作流函数调用、任务控制与回放管理

-

    修改标识：Senparc - 20260829
    修改描述：v0.3.0 新增工作流分析查询与管理端可视化

    修改标识：Senparc - 20260913
    修改描述：v0.4.0 新增 Chat 消息服务：历史查询、清理与 30 天保留期

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
    /// 只保存运行状态字段。运行时可能持有开始执行时读取的旧工作流定义，不能通过普通实体保存
    /// 把旧的 GraphJson、Revision 或编辑字段覆盖回数据库。
    /// </summary>
    public Task SaveRuntimeStartedAsync(WorkflowEntity workflow) =>
        SaveRuntimePropertiesAsync(workflow,
            nameof(WorkflowEntity.LastRunAt),
            nameof(WorkflowEntity.NextRunAt),
            nameof(WorkflowEntity.LastUpdateTime));

    public Task SaveRuntimeCompletedAsync(WorkflowEntity workflow) =>
        SaveRuntimePropertiesAsync(workflow,
            nameof(WorkflowEntity.LastSucceeded),
            nameof(WorkflowEntity.LastError),
            nameof(WorkflowEntity.LastUpdateTime));

    public async Task<IReadOnlyDictionary<int, string>> GetNameMapAsync(
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        return await BaseData.BaseDB.BaseDataContext.Set<WorkflowEntity>()
            .AsNoTracking()
            .Where(z => z.AdminUserId == adminUserId)
            .Select(z => new { z.Id, z.Name })
            .ToDictionaryAsync(z => z.Id, z => z.Name, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task SaveRuntimePropertiesAsync(WorkflowEntity workflow, params string[] propertyNames)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        var context = BaseData.BaseDB.BaseDataContext;
        var entry = context.Entry(workflow);
        context.ChangeTracker.DetectChanges();
        var runtimeProperties = propertyNames.ToHashSet(StringComparer.Ordinal);
        foreach (var property in entry.Properties)
        {
            property.IsModified = runtimeProperties.Contains(property.Metadata.Name);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
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

    public async Task<NeuCharWorkflowExecutionLog?> GetLatestReplaySnapshotAsync(int workflowId)
    {
        var logs = await GetFullListAsync(
            log => log.WorkflowId == workflowId,
            log => log.StartedAt,
            OrderingType.Descending).ConfigureAwait(false);
        return logs.FirstOrDefault(log => !string.IsNullOrWhiteSpace(log.ReplaySnapshotJson));
    }

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

    public async Task<IReadOnlyList<string>> GetRecentCompletedReplayEventsAsync(
        int workflowId,
        int maxLogs = 5,
        CancellationToken cancellationToken = default)
    {
        if (workflowId <= 0 || maxLogs <= 0)
        {
            return Array.Empty<string>();
        }

        return await BaseData.BaseDB.BaseDataContext.Set<NeuCharWorkflowExecutionLog>()
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
        var query = BaseData.BaseDB.BaseDataContext.Set<NeuCharWorkflowExecutionLog>()
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
        var query = BaseData.BaseDB.BaseDataContext.Set<NeuCharWorkflowExecutionLog>()
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
        var query = BaseData.BaseDB.BaseDataContext.Set<NeuCharWorkflowExecutionLog>()
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

        return BaseData.BaseDB.BaseDataContext.Set<NeuCharWorkflowExecutionLog>()
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
        return await BaseData.BaseDB.BaseDataContext.Set<NeuCharWorkflowExecutionLog>()
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

        return BaseData.BaseDB.BaseDataContext.Set<NeuCharWorkflowExecutionLog>()
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
/// Chat 消息持久化服务。消息历史保存到数据库，Host 重启后可恢复；
/// 保留期为 <see cref="RetentionDays"/> 天，过期消息由托管服务定期清理。
/// </summary>
public sealed class NeuCharWorkflowChatMessageService : WorkflowClientServiceBase<NeuCharWorkflowChatMessage>
{
    /// <summary>Chat 消息保留期（天）。</summary>
    public const int RetentionDays = 30;
    /// <summary>单次恢复写入内存会话的最大历史条数。</summary>
    public const int MaxHistoryCount = 200;

    public NeuCharWorkflowChatMessageService(
        INeuCharWorkflowChatMessageRepository repository,
        IServiceProvider serviceProvider)
        : base(repository, serviceProvider) { }

    /// <summary>
    /// 按发送顺序返回指定会话最近 <see cref="MaxHistoryCount"/> 条持久化消息。
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

    /// <summary>删除单个会话的全部持久化消息（会话重置时调用）。</summary>
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

    /// <summary>删除工作流的全部 Chat 消息（工作流删除时调用，与版本和执行日志清理保持一致）。</summary>
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
    /// 清理超过保留期的消息并返回删除条数；cap 限制单轮删除数量，避免重启后一次性大量删除。
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
