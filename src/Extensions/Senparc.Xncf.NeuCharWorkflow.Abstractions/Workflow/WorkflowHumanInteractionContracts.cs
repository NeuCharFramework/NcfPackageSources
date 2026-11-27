/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkflowHumanInteractionContracts.cs
    File description: Related implementation.
    Change description: Updated module behavior and related support.

    Created by: Senparc - 20260815

    Modified by: Senparc - 20260817
    Change description: v0.2.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.2.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.3.2 Localized Workflow designer and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.3.2.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;

/// <summary>
/// Describes a human approval or input request raised while a Workflow execution is paused.
/// The correlation and participant fields are used to ensure that a response is applied to
/// the intended execution and user.
/// </summary>
/// <param name="RequestId">The stable identifier of the pending interaction request.</param>
/// <param name="ChatTaskId">The related AgentsManager chat-task identifier.</param>
/// <param name="CorrelationId">The Workflow run correlation identifier.</param>
/// <param name="RequestType">The interaction kind, such as approval or input.</param>
/// <param name="AgentName">The Agent display name that requested the interaction.</param>
/// <param name="ToolName">The tool or function that is awaiting human handling.</param>
/// <param name="ToolArguments">The serialized arguments supplied to the tool.</param>
/// <param name="Prompt">The prompt displayed to the human participant.</param>
/// <param name="ParticipantKey">The stable participant key used to scope the request.</param>
/// <param name="NeuBellItemId">The optional NeuBell item identifier used for notification consumption.</param>
/// <param name="CreatedAt">The UTC timestamp at which the interaction was created.</param>
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

/// <summary>
/// Reports the outcome of resolving a pending Workflow human interaction.
/// </summary>
/// <param name="Success">Whether the bridge found and resolved the requested interaction.</param>
/// <param name="Approved">Whether the human approved the requested operation.</param>
/// <param name="Input">Optional human-provided input when the interaction requests data.</param>
/// <param name="Reason">Optional rejection or resolution reason.</param>
/// <param name="Message">A caller-facing status message.</param>
public sealed record WorkflowHumanInteractionResult(
    bool Success,
    bool Approved,
    string? Input,
    string? Reason,
    string Message);

/// <summary>
/// Defines the bridge used by Workflow to query and resolve human-in-the-loop requests.
/// A host may provide a real implementation through another module.
/// </summary>
public interface IWorkflowHumanInteractionBridge
{
    /// <summary>
    /// Returns pending interactions visible to one Workflow correlation and user.
    /// </summary>
    /// <param name="correlationId">The Workflow run correlation identifier.</param>
    /// <param name="userId">The authenticated user identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the lookup.</param>
    /// <returns>The pending interactions visible to the requested user.</returns>
    ValueTask<IReadOnlyList<WorkflowHumanInteraction>> GetPendingAsync(
        string correlationId,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies an approval or input response to one pending interaction.
    /// </summary>
    /// <param name="correlationId">The Workflow run correlation identifier.</param>
    /// <param name="userId">The authenticated user identifier.</param>
    /// <param name="requestId">The pending interaction identifier.</param>
    /// <param name="approved">Whether the human approves the requested operation.</param>
    /// <param name="input">Optional input supplied by the human.</param>
    /// <param name="reason">Optional rejection or resolution reason.</param>
    /// <param name="cancellationToken">Token used to cancel resolution.</param>
    /// <returns>The result describing whether the interaction was resolved.</returns>
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
/// Fallback bridge used when no module provides human-in-the-loop execution support.
/// It exposes no pending requests and returns a failed resolution result.
/// </summary>
public sealed class NullWorkflowHumanInteractionBridge : IWorkflowHumanInteractionBridge
{
    /// <inheritdoc />
    public ValueTask<IReadOnlyList<WorkflowHumanInteraction>> GetPendingAsync(
        string correlationId,
        string userId,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IReadOnlyList<WorkflowHumanInteraction>>(
            Array.Empty<WorkflowHumanInteraction>());

    /// <inheritdoc />
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
