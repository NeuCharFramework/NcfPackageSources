using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel;

[Table(Register.DATABASE_PREFIX + nameof(NeuCharWorkflow))]
[Serializable]
public class NeuCharWorkflow : EntityBase<int>
{
    [Required, MaxLength(200)]
    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }
    public string GraphJson { get; private set; } = "{\"nodes\":[],\"edges\":[]}";
    public int AdminUserId { get; private set; }
    public bool Enabled { get; private set; }
    [MaxLength(40)] public string TriggerType { get; private set; } = "manual";
    public string TriggerConfigJson { get; private set; } = "{}";
    public DateTime? NextRunAt { get; private set; }
    public DateTime? LastRunAt { get; private set; }
    public bool? LastSucceeded { get; private set; }
    public string? LastError { get; private set; }
    public int Revision { get; private set; }
    public int AutoSaveMinutes { get; private set; } = 3;
    [MaxLength(100)] public string? LegacySourceKey { get; private set; }

    private NeuCharWorkflow() { }

    public NeuCharWorkflow(string name, int adminUserId)
    {
        Name = name.Trim();
        AdminUserId = adminUserId;
    }

    public void Update(string name, string? description, string graphJson, bool enabled,
        string? triggerType, string? triggerConfigJson, DateTime? nextRunAt, int autoSaveMinutes)
    {
        Name = name.Trim();
        Description = description?.Trim();
        GraphJson = graphJson;
        Enabled = enabled;
        TriggerType = string.IsNullOrWhiteSpace(triggerType) ? "manual" : triggerType;
        TriggerConfigJson = triggerConfigJson ?? "{}";
        NextRunAt = enabled ? nextRunAt : null;
        AutoSaveMinutes = autoSaveMinutes <= 0 ? 0 : Math.Clamp(autoSaveMinutes, 1, 1440);
        Revision++;
        SetUpdateTime();
    }

    public void MarkStarted(DateTime? nextRunAt)
    {
        LastRunAt = DateTime.UtcNow;
        NextRunAt = nextRunAt;
        SetUpdateTime();
    }

    public void MarkCompleted(bool succeeded, string? error)
    {
        LastSucceeded = succeeded;
        LastError = succeeded ? null : Truncate(error, 4000);
        SetUpdateTime();
    }

    /// <summary>仅由从 Admin 旧表切换时使用，保留旧数据的审计时间与幂等来源键。</summary>
    public void RestoreFromLegacy(
        string legacySourceKey,
        DateTime? lastRunAt,
        bool? lastSucceeded,
        string? lastError,
        int revision,
        bool flag,
        DateTime addTime,
        DateTime lastUpdateTime,
        int tenantId,
        string? adminRemark,
        string? remark)
    {
        LegacySourceKey = legacySourceKey;
        LastRunAt = lastRunAt;
        LastSucceeded = lastSucceeded;
        LastError = lastError;
        Revision = Math.Max(0, revision);
        Flag = flag;
        AddTime = addTime;
        LastUpdateTime = lastUpdateTime;
        TenantId = tenantId;
        AdminRemark = adminRemark;
        Remark = remark;
    }

    private static string? Truncate(string? value, int length) =>
        string.IsNullOrEmpty(value) || value.Length <= length ? value : value[..length];
}

[Table(Register.DATABASE_PREFIX + nameof(NeuCharWorkflowVersion))]
[Serializable]
public class NeuCharWorkflowVersion : EntityBase<int>
{
    public int WorkflowId { get; private set; }
    public int Revision { get; private set; }
    [Required, MaxLength(200)] public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string GraphJson { get; private set; } = string.Empty;
    public bool Enabled { get; private set; }
    [MaxLength(40)] public string TriggerType { get; private set; } = string.Empty;
    public string TriggerConfigJson { get; private set; } = "{}";
    public int AutoSaveMinutes { get; private set; }
    public int AdminUserId { get; private set; }
    [Required, MaxLength(20)] public string SaveSource { get; private set; } = "manual";
    [MaxLength(100)] public string? LegacySourceKey { get; private set; }

    private NeuCharWorkflowVersion() { }

    public NeuCharWorkflowVersion(NeuCharWorkflow workflow, int adminUserId, string? saveSource)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        WorkflowId = workflow.Id;
        Revision = workflow.Revision;
        Name = workflow.Name;
        Description = workflow.Description;
        GraphJson = workflow.GraphJson;
        Enabled = workflow.Enabled;
        TriggerType = workflow.TriggerType;
        TriggerConfigJson = workflow.TriggerConfigJson;
        AutoSaveMinutes = workflow.AutoSaveMinutes;
        AdminUserId = adminUserId;
        SaveSource = saveSource?.Trim().ToLowerInvariant() switch
        {
            "auto" => "auto",
            "shortcut" => "shortcut",
            _ => "manual"
        };
    }

    /// <summary>仅用于 Admin 历史版本的幂等迁移。</summary>
    public void RestoreFromLegacy(
        string legacySourceKey,
        string name,
        string? description,
        string graphJson,
        bool enabled,
        string triggerType,
        string triggerConfigJson,
        int autoSaveMinutes,
        int revision,
        bool flag,
        DateTime addTime,
        DateTime lastUpdateTime,
        int tenantId,
        string? adminRemark,
        string? remark)
    {
        LegacySourceKey = legacySourceKey;
        Name = name;
        Description = description;
        GraphJson = graphJson;
        Enabled = enabled;
        TriggerType = triggerType;
        TriggerConfigJson = triggerConfigJson;
        AutoSaveMinutes = autoSaveMinutes;
        Revision = Math.Max(0, revision);
        Flag = flag;
        AddTime = addTime;
        LastUpdateTime = lastUpdateTime;
        TenantId = tenantId;
        AdminRemark = adminRemark;
        Remark = remark;
    }
}

[Table(Register.DATABASE_PREFIX + nameof(NeuCharWorkflowExecutionLog))]
[Serializable]
public class NeuCharWorkflowExecutionLog : EntityBase<int>
{
    public int WorkflowId { get; private set; }
    [MaxLength(200)] public string WorkflowName { get; private set; } = string.Empty;
    [MaxLength(100)] public string CorrelationId { get; private set; } = string.Empty;
    public DateTime StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public bool? Succeeded { get; private set; }
    public string? ResultSummary { get; private set; }
    public string? Error { get; private set; }
    /// <summary>运行时工作流定义的内容哈希；相同哈希会复用上一份快照，避免重复保存大图数据。</summary>
    [MaxLength(64)] public string? ReplaySnapshotHash { get; private set; }
    /// <summary>仅在与上一份快照不同的运行中保存完整定义。</summary>
    public string? ReplaySnapshotJson { get; private set; }
    /// <summary>按执行顺序保存的节点事件，用于完成后的只读回看。</summary>
    public string? ReplayEventsJson { get; private set; }

    private NeuCharWorkflowExecutionLog() { }

    public NeuCharWorkflowExecutionLog(int workflowId, string workflowName, string correlationId)
    {
        WorkflowId = workflowId;
        WorkflowName = workflowName;
        CorrelationId = correlationId;
        StartedAt = DateTime.UtcNow;
    }

    public void SetReplaySnapshot(string snapshotHash, string? snapshotJson)
    {
        ReplaySnapshotHash = Truncate(snapshotHash, 64);
        ReplaySnapshotJson = snapshotJson;
    }

    public void Complete(bool succeeded, string? resultSummary, string? error, string? replayEventsJson = null)
    {
        FinishedAt = DateTime.UtcNow;
        Succeeded = succeeded;
        ResultSummary = Truncate(resultSummary, 8000);
        Error = Truncate(error, 8000);
        ReplayEventsJson = replayEventsJson;
        SetUpdateTime();
    }

    private static string? Truncate(string? value, int length) =>
        string.IsNullOrEmpty(value) || value.Length <= length ? value : value[..length];
}

/// <summary>运行时冻结的工作流定义。该定义与实时编辑器相互独立，只用于任务回看和从回看复制草稿。</summary>
public sealed record NeuCharWorkflowReplayDefinition(
    string Name,
    string? Description,
    string GraphJson,
    bool Enabled,
    string TriggerType,
    string TriggerConfigJson,
    int AutoSaveMinutes,
    int Revision);
