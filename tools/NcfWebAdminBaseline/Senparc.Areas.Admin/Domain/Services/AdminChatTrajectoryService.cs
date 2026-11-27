/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatTrajectoryService.cs
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
using System.Linq;
using System.Threading.Tasks;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Service;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class AdminChatTrajectoryService : BaseClientService<AdminChatTrajectory>
{
    private readonly AdminChatTrajectoryEventService _eventService;

    /// <summary>
    /// Initializes trajectory persistence and its event persistence service.
    /// </summary>
    /// <param name="repository">The trajectory repository.</param>
    /// <param name="eventService">The trajectory-event service.</param>
    /// <param name="serviceProvider">The dependency provider used by the base service.</param>
    public AdminChatTrajectoryService(
        IAdminChatTrajectoryRepository repository,
        AdminChatTrajectoryEventService eventService,
        IServiceProvider serviceProvider)
        : base(repository, serviceProvider)
    {
        _eventService = eventService;
    }

    /// <summary>
    /// Creates and persists a trajectory for a chat session, optionally linking it
    /// to a parent trajectory and fork sequence.
    /// </summary>
    /// <param name="sessionId">The chat session identifier.</param>
    /// <param name="userId">The owning administrator identifier.</param>
    /// <param name="title">The trajectory title.</param>
    /// <param name="mode">The execution mode.</param>
    /// <param name="modelIdentifier">Optional model identifier.</param>
    /// <param name="parentTrajectoryId">Optional parent trajectory identifier.</param>
    /// <param name="forkFromSequence">Optional parent sequence used as the fork point.</param>
    /// <returns>The persisted trajectory.</returns>
    public async Task<AdminChatTrajectory> CreateAsync(
        int sessionId,
        int userId,
        string title,
        AdminChatMode mode,
        string modelIdentifier = null,
        int? parentTrajectoryId = null,
        int? forkFromSequence = null)
    {
        var trajectory = new AdminChatTrajectory(
            sessionId,
            userId,
            title,
            mode,
            modelIdentifier,
            parentTrajectoryId,
            forkFromSequence);
        await SaveObjectAsync(trajectory);
        return trajectory;
    }

    /// <summary>
    /// Loads a trajectory only when it belongs to the supplied administrator.
    /// </summary>
    /// <param name="trajectoryId">The trajectory identifier.</param>
    /// <param name="userId">The owning administrator identifier.</param>
    /// <returns>The owned trajectory, or <see langword="null"/>.</returns>
    public Task<AdminChatTrajectory> GetAsync(int trajectoryId, int userId)
    {
        return GetObjectAsync(z => z.Id == trajectoryId && z.UserId == userId);
    }

    /// <summary>
    /// Lists trajectories for an owner-scoped session newest first.
    /// </summary>
    /// <param name="sessionId">The chat session identifier.</param>
    /// <param name="userId">The owning administrator identifier.</param>
    /// <returns>The matching trajectories.</returns>
    public async Task<IReadOnlyList<AdminChatTrajectory>> GetSessionTrajectoriesAsync(int sessionId, int userId)
    {
        var trajectories = await GetFullListAsync(
            z => z.SessionId == sessionId && z.UserId == userId,
            "AddTime DESC");
        return trajectories.ToList();
    }

    /// <summary>
    /// Appends an event with the next trajectory sequence, persists the event,
    /// and advances the trajectory sequence marker.
    /// </summary>
    /// <param name="trajectory">The trajectory receiving the event.</param>
    /// <param name="eventType">The event category.</param>
    /// <param name="source">The event source.</param>
    /// <param name="name">The event name.</param>
    /// <param name="content">The human-readable event content.</param>
    /// <param name="payloadJson">The structured event payload.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="isReplayable">Whether the event may be replayed to clients.</param>
    /// <returns>The persisted trajectory event.</returns>
    public async Task<AdminChatTrajectoryEvent> AppendAsync(
        AdminChatTrajectory trajectory,
        string eventType,
        string source,
        string name,
        string content,
        string payloadJson,
        string correlationId = null,
        bool isReplayable = true)
    {
        var sequence = trajectory.NextSequence();
        var item = new AdminChatTrajectoryEvent(
            trajectory.Id,
            sequence,
            eventType,
            source,
            name,
            content,
            payloadJson,
            correlationId,
            isReplayable);
        await _eventService.SaveObjectAsync(item);
        trajectory.MarkSequence(sequence);
        await SaveObjectAsync(trajectory);
        return item;
    }

    /// <summary>
    /// Returns owner-scoped trajectory events before an optional sequence boundary,
    /// optionally filtering event fields by keyword and bounding the page size.
    /// </summary>
    /// <param name="trajectoryId">The trajectory identifier.</param>
    /// <param name="userId">The owning administrator identifier.</param>
    /// <param name="beforeSequence">Optional exclusive upper sequence boundary.</param>
    /// <param name="query">Optional case-insensitive event search text.</param>
    /// <param name="pageSize">The maximum number of events to return.</param>
    /// <returns>The matching events in ascending sequence order.</returns>
    public async Task<IReadOnlyList<AdminChatTrajectoryEvent>> GetEventsAsync(
        int trajectoryId,
        int userId,
        int? beforeSequence = null,
        string query = null,
        int pageSize = 200)
    {
        var trajectory = await GetAsync(trajectoryId, userId);
        if (trajectory == null)
        {
            return Array.Empty<AdminChatTrajectoryEvent>();
        }

        var events = (await _eventService.GetFullListAsync(
            z => z.TrajectoryId == trajectoryId
                && (!beforeSequence.HasValue || z.Sequence < beforeSequence.Value),
            "Sequence ASC")).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var keyword = query.Trim();
            events = events.Where(z =>
                    (z.EventType?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (z.Source?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (z.Name?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (z.Content?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (z.PayloadJson?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        return events.Take(Math.Max(1, pageSize)).ToList();
    }

    /// <summary>
    /// Searches owner-scoped trajectories by title, last error, and event content.
    /// </summary>
    /// <param name="userId">The owning administrator identifier.</param>
    /// <param name="query">The optional search text.</param>
    /// <param name="pageSize">The maximum number of matching trajectories.</param>
    /// <returns>The matching trajectories newest first.</returns>
    public async Task<IReadOnlyList<AdminChatTrajectory>> SearchAsync(
        int userId,
        string query,
        int pageSize = 50)
    {
        var trajectories = await GetFullListAsync(z => z.UserId == userId, "AddTime DESC");
        if (string.IsNullOrWhiteSpace(query))
        {
            return trajectories.Take(Math.Max(1, pageSize)).ToList();
        }

        var keyword = query.Trim();
        var matches = new List<AdminChatTrajectory>();
        foreach (var trajectory in trajectories)
        {
            if ((trajectory.Title?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                || (trajectory.LastError?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                matches.Add(trajectory);
                continue;
            }

            var events = (await _eventService.GetFullListAsync(
                z => z.TrajectoryId == trajectory.Id,
                "Sequence DESC")).AsEnumerable();
            if (events.Any(z =>
                    (z.EventType?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (z.Source?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (z.Name?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (z.Content?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (z.PayloadJson?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)))
            {
                matches.Add(trajectory);
            }

            if (matches.Count >= Math.Max(1, pageSize))
            {
                break;
            }
        }

        return matches;
    }

    /// <summary>
    /// Replaces the persisted serialized agent session state.
    /// </summary>
    /// <param name="trajectory">The trajectory to update.</param>
    /// <param name="sessionStateJson">The serialized session state.</param>
    /// <returns>A task that completes after persistence.</returns>
    public async Task UpdateSessionStateAsync(AdminChatTrajectory trajectory, string sessionStateJson)
    {
        trajectory.SetSessionState(sessionStateJson);
        await SaveObjectAsync(trajectory);
    }

    /// <summary>
    /// Marks a trajectory completed and persists its terminal state.
    /// </summary>
    /// <param name="trajectory">The trajectory to complete.</param>
    public async Task MarkCompletedAsync(AdminChatTrajectory trajectory)
    {
        trajectory.Complete();
        await SaveObjectAsync(trajectory);
    }

    /// <summary>
    /// Marks a trajectory as waiting for human approval.
    /// </summary>
    /// <param name="trajectory">The trajectory to update.</param>
    public async Task MarkWaitingForApprovalAsync(AdminChatTrajectory trajectory)
    {
        trajectory.WaitForApproval();
        await SaveObjectAsync(trajectory);
    }

    /// <summary>
    /// Marks a trajectory failed and persists the exception message as its error state.
    /// </summary>
    /// <param name="trajectory">The trajectory to fail.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    public async Task MarkFailedAsync(AdminChatTrajectory trajectory, Exception exception)
    {
        trajectory.Fail(exception?.Message);
        await SaveObjectAsync(trajectory);
    }
}

public sealed class AdminChatTrajectoryEventService : BaseClientService<AdminChatTrajectoryEvent>
{
    public AdminChatTrajectoryEventService(
        IAdminChatTrajectoryEventRepository repository,
        IServiceProvider serviceProvider)
        : base(repository, serviceProvider)
    {
    }
}
