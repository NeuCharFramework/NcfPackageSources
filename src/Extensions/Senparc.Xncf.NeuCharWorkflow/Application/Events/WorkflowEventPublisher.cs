/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkflowEventPublisher.cs
    File description: Workflow orchestration, replay, Webhook, and parallel execution

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 improve workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.5.0.0 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Application.Events;

/// <summary>
/// Workflow publishes status notifications only. IEventBus returns after
/// writing the event to the channel instead of waiting for subscribers, so
/// save, run, and scheduling threads are not blocked by cross-module consumers.
/// </summary>
public sealed class WorkflowEventPublisher
{
    private readonly IEventBus _eventBus;

    /// <summary>
    /// Initializes the workflow event publisher.
    /// </summary>
    /// <param name="eventBus">The event bus receiving workflow change notifications.</param>
    public WorkflowEventPublisher(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    /// <summary>
    /// Publishes a non-blocking workflow-change event for subscribers in other modules.
    /// </summary>
    /// <param name="workflowId">The workflow identifier that changed.</param>
    /// <param name="changeType">The operation or state-change label.</param>
    /// <param name="adminUserId">The administrator associated with the change.</param>
    /// <param name="cancellationToken">The event-publish cancellation token.</param>
    /// <returns>A task-like value that completes after the event is written to the bus.</returns>
    public ValueTask PublishAsync(int workflowId, string changeType, int adminUserId,
        CancellationToken cancellationToken = default) =>
        _eventBus.PublishAsync(
            new WorkflowChangedEvent(workflowId, changeType, adminUserId, DateTimeOffset.UtcNow),
            cancellationToken);
}
