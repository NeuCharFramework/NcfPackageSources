/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowModels.cs
    File description: Data models, DTOs, and mapping definitions

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 improve workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20260913
    Change description: v0.4.0 add the Chat message persistence entity so history survives Host restarts

    Modified by: Senparc - 20260915
    Change description: v0.4.0 improve Chat trigger message persistence and recovery

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

----------------------------------------------------------------*/

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

    /// <summary>
    /// Initializes an empty workflow for ORM materialization.
    /// </summary>
    private NeuCharWorkflow() { }

    /// <summary>
    /// Creates a workflow owned by an administrator with default runtime settings.
    /// </summary>
    /// <param name="name">The display name of the workflow.</param>
    /// <param name="adminUserId">The administrator identifier that owns the workflow.</param>
    public NeuCharWorkflow(string name, int adminUserId)
    {
        Name = name.Trim();
        AdminUserId = adminUserId;
    }

    /// <summary>
    /// Updates editable workflow definition and scheduling settings, then advances the revision.
    /// </summary>
    /// <param name="name">The new workflow display name.</param>
    /// <param name="description">The optional workflow description.</param>
    /// <param name="graphJson">The serialized workflow graph.</param>
    /// <param name="enabled">Whether the workflow is enabled for triggers.</param>
    /// <param name="triggerType">The trigger type; blank values become <c>manual</c>.</param>
    /// <param name="triggerConfigJson">The serialized trigger configuration.</param>
    /// <param name="nextRunAt">The next scheduled time when enabled.</param>
    /// <param name="autoSaveMinutes">The requested autosave interval, clamped to the supported range.</param>
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

    /// <summary>
    /// Records that runtime execution has started and updates the next scheduled time.
    /// </summary>
    /// <param name="nextRunAt">The next scheduled execution time, if any.</param>
    public void MarkStarted(DateTime? nextRunAt)
    {
        LastRunAt = DateTime.UtcNow;
        NextRunAt = nextRunAt;
        SetUpdateTime();
    }

    /// <summary>
    /// Records the completion status and retains a bounded failure message on failure.
    /// </summary>
    /// <param name="succeeded">Whether execution completed successfully.</param>
    /// <param name="error">The failure message when execution did not succeed.</param>
    public void MarkCompleted(bool succeeded, string? error)
    {
        LastSucceeded = succeeded;
        LastError = succeeded ? null : Truncate(error, 4000);
        SetUpdateTime();
    }

    /// <summary>Used only when switching from the legacy Admin table; preserves audit timestamps and the idempotent source key.</summary>
    /// <param name="legacySourceKey">The legacy source key used for idempotent migration.</param>
    /// <param name="lastRunAt">The historical last-run timestamp.</param>
    /// <param name="lastSucceeded">The historical completion status.</param>
    /// <param name="lastError">The historical failure message.</param>
    /// <param name="revision">The historical workflow revision.</param>
    /// <param name="flag">The historical base-entity flag.</param>
    /// <param name="addTime">The historical creation timestamp.</param>
    /// <param name="lastUpdateTime">The historical update timestamp.</param>
    /// <param name="tenantId">The historical tenant identifier.</param>
    /// <param name="adminRemark">The historical administrator remark.</param>
    /// <param name="remark">The historical general remark.</param>
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

    /// <summary>
    /// Truncates persisted text to the specified maximum length.
    /// </summary>
    /// <param name="value">The text to bound.</param>
    /// <param name="length">The maximum character count.</param>
    /// <returns>The original or truncated text.</returns>
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

    /// <summary>
    /// Initializes an empty version for ORM materialization.
    /// </summary>
    private NeuCharWorkflowVersion() { }

    /// <summary>
    /// Captures the current workflow definition as a version snapshot.
    /// </summary>
    /// <param name="workflow">The live workflow to snapshot.</param>
    /// <param name="adminUserId">The administrator creating the snapshot.</param>
    /// <param name="saveSource">The save origin, normalized to a supported value.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="workflow"/> is null.</exception>
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

    /// <summary>Used only for idempotent migration from historical Admin versions.</summary>
    /// <param name="legacySourceKey">The legacy source key used for idempotent migration.</param>
    /// <param name="name">The historical workflow name.</param>
    /// <param name="description">The historical workflow description.</param>
    /// <param name="graphJson">The historical serialized graph.</param>
    /// <param name="enabled">The historical enabled flag.</param>
    /// <param name="triggerType">The historical trigger type.</param>
    /// <param name="triggerConfigJson">The historical trigger configuration.</param>
    /// <param name="autoSaveMinutes">The historical autosave interval.</param>
    /// <param name="revision">The historical revision.</param>
    /// <param name="flag">The historical base-entity flag.</param>
    /// <param name="addTime">The historical creation timestamp.</param>
    /// <param name="lastUpdateTime">The historical update timestamp.</param>
    /// <param name="tenantId">The historical tenant identifier.</param>
    /// <param name="adminRemark">The historical administrator remark.</param>
    /// <param name="remark">The historical general remark.</param>
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
    /// <summary>Content hash of the runtime workflow definition. The same hash reuses the previous snapshot to avoid storing large graph data repeatedly.</summary>
    [MaxLength(64)] public string? ReplaySnapshotHash { get; private set; }
    /// <summary>Stores the complete definition only when it differs from the previous snapshot.</summary>
    public string? ReplaySnapshotJson { get; private set; }
    /// <summary>Node events stored in execution order for read-only replay after completion.</summary>
    public string? ReplayEventsJson { get; private set; }

    /// <summary>
    /// Initializes an empty execution log for ORM materialization.
    /// </summary>
    private NeuCharWorkflowExecutionLog() { }

    /// <summary>
    /// Starts an execution log for a workflow correlation.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="workflowName">The workflow display name at execution start.</param>
    /// <param name="correlationId">The identifier used to correlate runtime events.</param>
    public NeuCharWorkflowExecutionLog(int workflowId, string workflowName, string correlationId)
    {
        WorkflowId = workflowId;
        WorkflowName = workflowName;
        CorrelationId = correlationId;
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Stores the bounded hash and optional serialized workflow definition used for replay.
    /// </summary>
    /// <param name="snapshotHash">The content hash of the runtime definition.</param>
    /// <param name="snapshotJson">The definition JSON when a new snapshot must be retained.</param>
    public void SetReplaySnapshot(string snapshotHash, string? snapshotJson)
    {
        ReplaySnapshotHash = Truncate(snapshotHash, 64);
        ReplaySnapshotJson = snapshotJson;
    }

    /// <summary>
    /// Completes the log and stores bounded result, error, and replay-event data.
    /// </summary>
    /// <param name="succeeded">Whether the execution succeeded.</param>
    /// <param name="resultSummary">The result summary to persist.</param>
    /// <param name="error">The failure message to persist.</param>
    /// <param name="replayEventsJson">Optional serialized node events.</param>
    public void Complete(bool succeeded, string? resultSummary, string? error, string? replayEventsJson = null)
    {
        FinishedAt = DateTime.UtcNow;
        Succeeded = succeeded;
        ResultSummary = Truncate(resultSummary, 8000);
        Error = Truncate(error, 8000);
        ReplayEventsJson = replayEventsJson;
        SetUpdateTime();
    }

    /// <summary>
    /// Truncates execution-log text to the specified maximum length.
    /// </summary>
    /// <param name="value">The text to bound.</param>
    /// <param name="length">The maximum character count.</param>
    /// <returns>The original or truncated text.</returns>
    private static string? Truncate(string? value, int length) =>
        string.IsNullOrEmpty(value) || value.Length <= length ? value : value[..length];
}

[Table(Register.DATABASE_PREFIX + nameof(NeuCharWorkflowChatMessage))]
[Serializable]
public class NeuCharWorkflowChatMessage : EntityBase<int>
{
    public int WorkflowId { get; private set; }
    /// <summary>SHA256 hexadecimal digest of the participant key (user:xxx or guest:token); only the digest is stored.</summary>
    [MaxLength(64)] public string ParticipantKeyHash { get; private set; } = string.Empty;
    /// <summary>Message role: user, assistant, or error.</summary>
    [MaxLength(16)] public string Role { get; private set; } = "user";
    /// <summary>Message content, truncated to 8000 characters on the server.</summary>
    public string Content { get; private set; } = string.Empty;
    /// <summary>Workflow run associated with this message; empty when no run was started.</summary>
    public Guid? RunId { get; private set; }

    /// <summary>
    /// Initializes an empty chat message for ORM materialization.
    /// </summary>
    private NeuCharWorkflowChatMessage() { }

    /// <summary>
    /// Creates a persisted workflow chat message with bounded participant, role, and content fields.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="participantKeyHash">The hashed participant key.</param>
    /// <param name="role">The message role.</param>
    /// <param name="content">The message content.</param>
    /// <param name="runId">The optional associated runtime run identifier.</param>
    public NeuCharWorkflowChatMessage(int workflowId, string participantKeyHash, string role, string content, Guid? runId)
    {
        WorkflowId = workflowId;
        ParticipantKeyHash = Truncate(participantKeyHash, 64) ?? string.Empty;
        Role = Truncate(role, 16) ?? "user";
        Content = Truncate(content, 8000) ?? string.Empty;
        RunId = runId;
    }

    /// <summary>
    /// Truncates chat text to the specified maximum length.
    /// </summary>
    /// <param name="value">The text to bound.</param>
    /// <param name="length">The maximum character count.</param>
    /// <returns>The original or truncated text.</returns>
    private static string? Truncate(string? value, int length) =>
        string.IsNullOrEmpty(value) || value.Length <= length ? value : value[..length];
}

/// <summary>Frozen workflow definition captured at runtime. It is independent from the live editor and is used only for task replay and draft copying.</summary>
public sealed record NeuCharWorkflowReplayDefinition(
    string Name,
    string? Description,
    string GraphJson,
    bool Enabled,
    string TriggerType,
    string TriggerConfigJson,
    int AutoSaveMinutes,
    int Revision);
