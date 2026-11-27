/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkflowModuleEvents.cs
    File description: Related implementation.

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.3.2 Localized Workflow designer and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.3.2.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Events;
using System;

namespace Senparc.Xncf.NeuCharWorkflow.Abstractions.Events;

/// <summary>
/// Reports that a Workflow definition or runtime state changed.
/// </summary>
/// <param name="WorkflowId">The affected Workflow identifier.</param>
/// <param name="ChangeType">The operation or state change name.</param>
/// <param name="AdminUserId">The administrator associated with the change.</param>
/// <param name="OccurredAt">The UTC timestamp of the change.</param>
public sealed record WorkflowChangedEvent(
    int WorkflowId,
    string ChangeType,
    int AdminUserId,
    DateTimeOffset OccurredAt) : IntegrationEvent
{
    public override string GetEventSummary() =>
        $"WorkflowChanged[{WorkflowId}] {ChangeType}";
}
