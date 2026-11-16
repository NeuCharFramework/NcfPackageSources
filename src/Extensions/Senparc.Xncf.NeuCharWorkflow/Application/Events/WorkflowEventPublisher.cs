/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: WorkflowEventPublisher.cs
    File description: Workflow orchestration, replay, Webhook, and parallel execution


    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 improve workflow orchestration, replay, Webhook, and parallel execution

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

    public WorkflowEventPublisher(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public ValueTask PublishAsync(int workflowId, string changeType, int adminUserId,
        CancellationToken cancellationToken = default) =>
        _eventBus.PublishAsync(
            new WorkflowChangedEvent(workflowId, changeType, adminUserId, DateTimeOffset.UtcNow),
            cancellationToken);
}
