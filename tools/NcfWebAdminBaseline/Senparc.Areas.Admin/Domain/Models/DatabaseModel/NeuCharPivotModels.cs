/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharPivotModels.cs
    File description: Related implementation.

    Created by: Senparc - 20260809

    Modified by: Senparc - 20260813
    Change description: v0.5.0 Updated module behavior and related support.

    Modified by: Senparc - 20260829
    Change description: v0.7.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel;

[Table(Register.DATABASE_PREFIX + nameof(NeuCharPivotConfiguration))]
[Serializable]
public class NeuCharPivotConfiguration : EntityBase<int>
{
    [Required, MaxLength(100)]
    public string ModuleUid { get; private set; }

    [Required, MaxLength(200)]
    public string Name { get; private set; }

    public string UserRequirement { get; private set; }

    public string LayoutSchemaJson { get; private set; }

    public int AiModelId { get; private set; }

    public int AdminUserId { get; private set; }

    public int? ChatSessionId { get; private set; }

    public int Revision { get; private set; }

    public DateTime? LastGeneratedAt { get; private set; }

    public string LastError { get; private set; }

    private NeuCharPivotConfiguration() { }

    /// <summary>
    /// Initializes a pivot configuration for a module and administrator.
    /// </summary>
    /// <param name="moduleUid">The target module UID.</param>
    /// <param name="name">The initial configuration name.</param>
    /// <param name="adminUserId">The owning administrator identifier.</param>
    public NeuCharPivotConfiguration(string moduleUid, string name, int adminUserId)
    {
        ModuleUid = moduleUid;
        Name = name;
        AdminUserId = adminUserId;
        LayoutSchemaJson = string.Empty;
        UserRequirement = string.Empty;
        Revision = 0;
    }

    /// <summary>
    /// Applies a generated layout and generation metadata, increments the revision,
    /// and clears the previous error.
    /// </summary>
    /// <param name="name">The generated display name.</param>
    /// <param name="requirement">The user requirement.</param>
    /// <param name="layoutSchemaJson">The normalized layout JSON.</param>
    /// <param name="aiModelId">The model identifier used.</param>
    /// <param name="adminUserId">The administrator owner.</param>
    /// <param name="chatSessionId">The optional source chat session.</param>
    public void ApplyGeneratedLayout(
        string name,
        string requirement,
        string layoutSchemaJson,
        int aiModelId,
        int adminUserId,
        int? chatSessionId)
    {
        Name = string.IsNullOrWhiteSpace(name) ? Name : name.Trim();
        UserRequirement = requirement?.Trim() ?? string.Empty;
        LayoutSchemaJson = layoutSchemaJson ?? string.Empty;
        AiModelId = aiModelId;
        AdminUserId = adminUserId;
        ChatSessionId = chatSessionId;
        Revision++;
        LastGeneratedAt = DateTime.UtcNow;
        LastError = null;
        SetUpdateTime();
    }

    /// <summary>
    /// Records a bounded generation error and updates the entity timestamp.
    /// </summary>
    /// <param name="error">The error detail.</param>
    public void RecordError(string error)
    {
        LastError = Truncate(error, 4000);
        SetUpdateTime();
    }

    private static string Truncate(string value, int length) =>
        string.IsNullOrEmpty(value) || value.Length <= length ? value : value[..length];
}

[Table(Register.DATABASE_PREFIX + nameof(NeuCharPivotFunction))]
[Serializable]
public class NeuCharPivotFunction : EntityBase<int>
{
    public int PivotId { get; private set; }

    [Required, MaxLength(100)]
    public string ModuleUid { get; private set; }

    [Required, MaxLength(200)]
    public string FunctionKey { get; private set; }

    [Required, MaxLength(200)]
    public string FunctionName { get; private set; }

    public string Description { get; private set; }

    public string UiSchemaJson { get; private set; }

    public string DefaultParametersJson { get; private set; }

    [MaxLength(100)]
    public string ModuleVersion { get; private set; }

    public int Sort { get; private set; }

    public bool Visible { get; private set; }

    private NeuCharPivotFunction() { }

    /// <summary>
    /// Initializes a persisted pivot Function row with visible/default JSON state.
    /// </summary>
    /// <param name="pivotId">The parent pivot configuration identifier.</param>
    /// <param name="moduleUid">The owning module UID.</param>
    /// <param name="functionKey">The live Function key.</param>
    /// <param name="functionName">The Function display name.</param>
    /// <param name="description">The Function description.</param>
    public NeuCharPivotFunction(
        int pivotId,
        string moduleUid,
        string functionKey,
        string functionName,
        string description)
    {
        PivotId = pivotId;
        ModuleUid = moduleUid;
        FunctionKey = functionKey;
        FunctionName = functionName;
        Description = description;
        UiSchemaJson = "{}";
        DefaultParametersJson = "{}";
        Visible = true;
    }

    /// <summary>
    /// Updates Function display metadata, UI/default schemas, module version,
    /// ordering, and visibility.
    /// </summary>
    /// <param name="functionName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="uiSchemaJson">The UI parameter schema JSON.</param>
    /// <param name="defaultParametersJson">The default parameter JSON.</param>
    /// <param name="moduleVersion">The source module version.</param>
    /// <param name="sort">The display order.</param>
    /// <param name="visible">Whether the Function is visible.</param>
    public void Update(
        string functionName,
        string description,
        string uiSchemaJson,
        string defaultParametersJson,
        string moduleVersion,
        int sort,
        bool visible)
    {
        FunctionName = functionName;
        Description = description;
        UiSchemaJson = uiSchemaJson ?? "{}";
        DefaultParametersJson = defaultParametersJson ?? "{}";
        ModuleVersion = moduleVersion;
        Sort = sort;
        Visible = visible;
        SetUpdateTime();
    }
}

[Table(Register.DATABASE_PREFIX + nameof(NeuCharPivotLoopTask))]
[Serializable]
public class NeuCharPivotLoopTask : EntityBase<int>
{
    public int FunctionId { get; private set; }

    public int AdminUserId { get; private set; }

    public int IntervalSeconds { get; private set; }

    public string ParametersJson { get; private set; }

    public bool Enabled { get; private set; }

    public bool UseNeuBell { get; private set; }

    public int? WorkflowId { get; private set; }

    public DateTime? NextRunAt { get; private set; }

    public DateTime? LastRunAt { get; private set; }

    public bool? LastSucceeded { get; private set; }

    public int ConsecutiveFailures { get; private set; }

    public string LastError { get; private set; }

    private NeuCharPivotLoopTask() { }

    /// <summary>
    /// Initializes a loop task with the default five-minute interval and empty parameters.
    /// </summary>
    /// <param name="functionId">The pivot Function identifier.</param>
    /// <param name="adminUserId">The owning administrator identifier.</param>
    public NeuCharPivotLoopTask(int functionId, int adminUserId)
    {
        FunctionId = functionId;
        AdminUserId = adminUserId;
        IntervalSeconds = 300;
        ParametersJson = "{}";
    }

    /// <summary>
    /// Configures interval, protected parameters, enablement, NeuBell, and workflow routing.
    /// Enabled tasks receive their next scheduled execution time.
    /// </summary>
    /// <param name="intervalSeconds">The interval, clamped to 60 seconds through one year.</param>
    /// <param name="parametersJson">The protected execution parameters.</param>
    /// <param name="enabled">Whether scheduling is enabled.</param>
    /// <param name="useNeuBell">Whether completion notifications are enabled.</param>
    /// <param name="workflowId">Optional workflow identifier.</param>
    public void Configure(
        int intervalSeconds,
        string parametersJson,
        bool enabled,
        bool useNeuBell,
        int? workflowId = null)
    {
        IntervalSeconds = Math.Clamp(intervalSeconds, 60, 31_536_000);
        ParametersJson = parametersJson ?? "{}";
        Enabled = enabled;
        UseNeuBell = useNeuBell;
        WorkflowId = workflowId > 0 ? workflowId : null;
        NextRunAt = enabled ? DateTime.UtcNow.AddSeconds(IntervalSeconds) : null;
        if (!enabled)
        {
            ConsecutiveFailures = 0;
        }
        SetUpdateTime();
    }

    /// <summary>
    /// Records the current run start and schedules the next run using the configured interval.
    /// </summary>
    public void MarkStarted()
    {
        LastRunAt = DateTime.UtcNow;
        NextRunAt = LastRunAt.Value.AddSeconds(Math.Max(60, IntervalSeconds));
        SetUpdateTime();
    }

    /// <summary>
    /// Records completion outcome, resets or increments consecutive failures,
    /// and stores a bounded error when execution failed.
    /// </summary>
    /// <param name="succeeded">Whether execution succeeded.</param>
    /// <param name="error">The failure detail.</param>
    public void MarkCompleted(bool succeeded, string error)
    {
        LastSucceeded = succeeded;
        ConsecutiveFailures = succeeded ? 0 : ConsecutiveFailures + 1;
        LastError = succeeded ? null : Truncate(error, 4000);
        SetUpdateTime();
    }

    /// <summary>
    /// Disables scheduling after an unrecoverable task error and records the failure.
    /// </summary>
    /// <param name="error">The disabling error detail.</param>
    public void DisableForError(string error)
    {
        Enabled = false;
        NextRunAt = null;
        LastSucceeded = false;
        ConsecutiveFailures++;
        LastError = Truncate(error, 4000);
        SetUpdateTime();
    }

    private static string Truncate(string value, int length) =>
        string.IsNullOrEmpty(value) || value.Length <= length ? value : value[..length];
}

[Table(Register.DATABASE_PREFIX + nameof(NeuCharExecutionLog))]
[Serializable]
public class NeuCharExecutionLog : EntityBase<int>
{
    [Required, MaxLength(40)]
    public string SourceType { get; private set; }

    public int SourceId { get; private set; }

    [MaxLength(100)]
    public string ModuleUid { get; private set; }

    [MaxLength(200)]
    public string FunctionKey { get; private set; }

    [MaxLength(200)]
    public string DisplayName { get; private set; }

    public DateTime StartedAt { get; private set; }

    public DateTime? FinishedAt { get; private set; }

    public bool? Succeeded { get; private set; }

    public string ResultSummary { get; private set; }

    public string Error { get; private set; }

    [MaxLength(100)]
    public string CorrelationId { get; private set; }

    private NeuCharExecutionLog() { }

    /// <summary>
    /// Starts an execution audit log with source, Function, display, and correlation metadata.
    /// </summary>
    /// <param name="sourceType">The execution source category.</param>
    /// <param name="sourceId">The source entity identifier.</param>
    /// <param name="moduleUid">The module UID.</param>
    /// <param name="functionKey">The Function key.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    public NeuCharExecutionLog(
        string sourceType,
        int sourceId,
        string moduleUid,
        string functionKey,
        string displayName,
        string correlationId)
    {
        SourceType = sourceType;
        SourceId = sourceId;
        ModuleUid = moduleUid;
        FunctionKey = functionKey;
        DisplayName = displayName;
        CorrelationId = correlationId;
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Closes an execution log with bounded result/error summaries and completion time.
    /// </summary>
    /// <param name="succeeded">Whether execution succeeded.</param>
    /// <param name="resultSummary">The result summary.</param>
    /// <param name="error">The error detail.</param>
    public void Complete(bool succeeded, string resultSummary, string error)
    {
        FinishedAt = DateTime.UtcNow;
        Succeeded = succeeded;
        ResultSummary = Truncate(resultSummary, 8000);
        Error = Truncate(error, 8000);
        SetUpdateTime();
    }

    private static string Truncate(string value, int length) =>
        string.IsNullOrEmpty(value) || value.Length <= length ? value : value[..length];
}
