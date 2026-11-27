/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatTrajectory.cs
    File description: Related implementation.

    Created by: Senparc - 20260915

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;
using Senparc.Areas.Admin.Domain.Services;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel;

/// <summary>
/// A durable Admin Chat Harness run. The event rows form an append-only trajectory.
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(AdminChatTrajectory))]
[Serializable]
public class AdminChatTrajectory : EntityBase<int>
{
    [Required]
    public int SessionId { get; private set; }

    [Required]
    public int UserId { get; private set; }

    public int? ParentTrajectoryId { get; private set; }

    public int? ForkFromSequence { get; private set; }

    [Required, MaxLength(150)]
    public string Title { get; private set; }

    [Required]
    public AdminChatMode Mode { get; private set; }

    [Required]
    public AdminChatTrajectoryStatus Status { get; private set; }

    [Required]
    public DateTime StartedAt { get; private set; }

    public DateTime? FinishedAt { get; private set; }

    [Required]
    public int LastSequence { get; private set; }

    [MaxLength(100)]
    public string ModelIdentifier { get; private set; }

    public string SessionStateJson { get; private set; }

    public string LastError { get; private set; }

    [ForeignKey(nameof(SessionId))]
    public virtual AdminChatSession Session { get; private set; }

    public virtual ICollection<AdminChatTrajectoryEvent> Events { get; private set; } = new List<AdminChatTrajectoryEvent>();

    private AdminChatTrajectory() { }

    /// <summary>
    /// Initializes a running Harness trajectory with optional parent/fork lineage.
    /// </summary>
    /// <param name="sessionId">The administrator chat session identifier.</param>
    /// <param name="userId">The administrator identifier owning the trajectory.</param>
    /// <param name="title">The display title, truncated to the entity limit when necessary.</param>
    /// <param name="mode">The Harness execution mode.</param>
    /// <param name="modelIdentifier">The optional model identifier used for execution.</param>
    /// <param name="parentTrajectoryId">The optional parent trajectory identifier.</param>
    /// <param name="forkFromSequence">The optional source sequence for a fork.</param>
    public AdminChatTrajectory(
        int sessionId,
        int userId,
        string title,
        AdminChatMode mode,
        string modelIdentifier = null,
        int? parentTrajectoryId = null,
        int? forkFromSequence = null)
    {
        SessionId = sessionId;
        UserId = userId;
        Title = string.IsNullOrWhiteSpace(title)
            ? "Harness 任务"
            : title.Length > 150 ? title.Substring(0, 150) : title;
        Mode = mode;
        Status = AdminChatTrajectoryStatus.Running;
        StartedAt = DateTime.Now;
        LastSequence = 0;
        ModelIdentifier = modelIdentifier;
        ParentTrajectoryId = parentTrajectoryId;
        ForkFromSequence = forkFromSequence;
    }

    /// <summary>
    /// Returns the next sequence number after the latest recorded trajectory event.
    /// </summary>
    /// <returns>The next sequence number.</returns>
    public int NextSequence() => LastSequence + 1;

    /// <summary>
    /// Advances <see cref="LastSequence"/> when the supplied sequence is greater and records the update time.
    /// </summary>
    /// <param name="sequence">The sequence number observed by the event writer.</param>
    public void MarkSequence(int sequence)
    {
        if (sequence > LastSequence)
        {
            LastSequence = sequence;
        }
        base.SetUpdateTime();
    }

    /// <summary>
    /// Replaces the serialized session state snapshot and records the update time.
    /// </summary>
    /// <param name="sessionStateJson">The serialized state payload.</param>
    public void SetSessionState(string sessionStateJson)
    {
        SessionStateJson = sessionStateJson;
        base.SetUpdateTime();
    }

    /// <summary>
    /// Marks the trajectory as completed and records its finish time.
    /// </summary>
    public void Complete()
    {
        Status = AdminChatTrajectoryStatus.Completed;
        FinishedAt = DateTime.Now;
        base.SetUpdateTime();
    }

    /// <summary>
    /// Marks the trajectory as waiting for an external approval decision.
    /// </summary>
    public void WaitForApproval()
    {
        Status = AdminChatTrajectoryStatus.WaitingForApproval;
        base.SetUpdateTime();
    }

    /// <summary>
    /// Marks the trajectory as failed, stores the diagnostic error, and records its finish time.
    /// </summary>
    /// <param name="error">The failure detail to retain.</param>
    public void Fail(string error)
    {
        Status = AdminChatTrajectoryStatus.Failed;
        LastError = error;
        FinishedAt = DateTime.Now;
        base.SetUpdateTime();
    }

    /// <summary>
    /// Marks the trajectory as cancelled and records its finish time.
    /// </summary>
    public void Cancel()
    {
        Status = AdminChatTrajectoryStatus.Cancelled;
        FinishedAt = DateTime.Now;
        base.SetUpdateTime();
    }
}

public enum AdminChatTrajectoryStatus
{
    Running = 0,
    WaitingForApproval = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

/// <summary>
/// One append-only event in an Admin Chat Harness trajectory.
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(AdminChatTrajectoryEvent))]
[Serializable]
public class AdminChatTrajectoryEvent : EntityBase<int>
{
    [Required]
    public int TrajectoryId { get; private set; }

    [Required]
    public int Sequence { get; private set; }

    [Required, MaxLength(50)]
    public string EventType { get; private set; }

    [MaxLength(100)]
    public string Source { get; private set; }

    [MaxLength(200)]
    public string Name { get; private set; }

    public string Content { get; private set; }

    public string PayloadJson { get; private set; }

    [Required]
    public DateTime OccurredAt { get; private set; }

    [MaxLength(100)]
    public string CorrelationId { get; private set; }

    [Required]
    public bool IsReplayable { get; private set; }

    [ForeignKey(nameof(TrajectoryId))]
    public virtual AdminChatTrajectory Trajectory { get; private set; }

    private AdminChatTrajectoryEvent() { }

    /// <summary>
    /// Initializes one append-only trajectory event with its sequence, payload, and replayability flag.
    /// </summary>
    /// <param name="trajectoryId">The owning trajectory identifier.</param>
    /// <param name="sequence">The event sequence within the trajectory.</param>
    /// <param name="eventType">The event type; null defaults to <c>system</c>.</param>
    /// <param name="source">The component that produced the event.</param>
    /// <param name="name">The event name.</param>
    /// <param name="content">The human-readable event content.</param>
    /// <param name="payloadJson">The serialized event payload.</param>
    /// <param name="correlationId">An optional correlation identifier.</param>
    /// <param name="isReplayable">Whether the event may be replayed.</param>
    public AdminChatTrajectoryEvent(
        int trajectoryId,
        int sequence,
        string eventType,
        string source,
        string name,
        string content,
        string payloadJson,
        string correlationId = null,
        bool isReplayable = true)
    {
        TrajectoryId = trajectoryId;
        Sequence = sequence;
        EventType = eventType ?? "system";
        Source = source;
        Name = name;
        Content = content;
        PayloadJson = payloadJson;
        OccurredAt = DateTime.Now;
        CorrelationId = correlationId;
        IsReplayable = isReplayable;
    }
}
