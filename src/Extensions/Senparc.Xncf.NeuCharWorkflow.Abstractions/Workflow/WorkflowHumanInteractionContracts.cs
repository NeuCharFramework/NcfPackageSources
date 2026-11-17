/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: WorkflowHumanInteractionContracts.cs
    File description: Related implementation.

    Created by: Senparc - 20260815
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.2.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.2.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public sealed record WorkflowHumanInteraction(
    string RequestId,
    int ChatTaskId,
    string CorrelationId,
    string RequestType,
    string AgentName,
    string ToolName,
    string ToolArguments,
    string Prompt,
    string ParticipantKey,
    string NeuBellItemId,
    DateTimeOffset CreatedAt);

public sealed record WorkflowHumanInteractionResult(
    bool Success,
    bool Approved,
    string? Input,
    string? Reason,
    string Message);

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public interface IWorkflowHumanInteractionBridge
{
    ValueTask<IReadOnlyList<WorkflowHumanInteraction>> GetPendingAsync(
        string correlationId,
        string userId,
        CancellationToken cancellationToken = default);

    ValueTask<WorkflowHumanInteractionResult> ResolveAsync(
        string correlationId,
        string userId,
        string requestId,
        bool approved,
        string? input = null,
        string? reason = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed class NullWorkflowHumanInteractionBridge : IWorkflowHumanInteractionBridge
{
    public ValueTask<IReadOnlyList<WorkflowHumanInteraction>> GetPendingAsync(
        string correlationId,
        string userId,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IReadOnlyList<WorkflowHumanInteraction>>(
            Array.Empty<WorkflowHumanInteraction>());

    public ValueTask<WorkflowHumanInteractionResult> ResolveAsync(
        string correlationId,
        string userId,
        string requestId,
        bool approved,
        string? input = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new WorkflowHumanInteractionResult(
            false,
            false,
            null,
            null,
            "当前未安装支持 Workflow Human-in-the-Loop 的执行模块。"));
}
