/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: WorkflowModuleEvents.cs
    File description: Related implementation.


    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Events;
using System;

namespace Senparc.Xncf.NeuCharWorkflow.Abstractions.Events;

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public sealed record WorkflowChangedEvent(
    int WorkflowId,
    string ChangeType,
    int AdminUserId,
    DateTimeOffset OccurredAt) : IntegrationEvent
{
    public override string GetEventSummary() =>
        $"WorkflowChanged[{WorkflowId}] {ChangeType}";
}
