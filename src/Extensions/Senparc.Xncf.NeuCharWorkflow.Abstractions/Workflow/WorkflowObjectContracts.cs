/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkflowObjectContracts.cs
    File description: Related implementation.

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.2.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.2.0 Updated module behavior and related support.

    Modified by: Senparc - 20260829
    Change description: v0.3.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.3.2 Localized Workflow designer and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.3.2.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;

/// <summary>
/// Describes an executable Workflow object exposed by a provider.
/// </summary>
/// <param name="ProviderId">The stable provider identifier.</param>
/// <param name="ObjectId">The provider-local object identifier.</param>
/// <param name="Kind">The object kind used by Workflow node selection.</param>
/// <param name="Name">The display name shown to administrators.</param>
/// <param name="Description">The object purpose and invocation behavior.</param>
/// <param name="Enabled">Whether the object may currently be selected or executed.</param>
/// <param name="Icon">An optional icon identifier.</param>
/// <param name="EditUrl">An optional provider-specific administration URL.</param>
/// <param name="Metadata">Optional provider metadata for filtering or display.</param>
public sealed record WorkflowObjectDescriptor(
    string ProviderId,
    string ObjectId,
    string Kind,
    string Name,
    string Description,
    bool Enabled,
    string? Icon = null,
    string? EditUrl = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

/// <summary>
/// Carries the input and execution context required to invoke a Workflow object.
/// </summary>
/// <param name="ObjectId">The provider-local object identifier.</param>
/// <param name="Input">The user or node input passed to the object.</param>
/// <param name="AiModelId">The AI model identifier selected for execution.</param>
/// <param name="CorrelationId">The Workflow run correlation identifier.</param>
/// <param name="Parameters">Optional execution parameters keyed by contract name.</param>
/// <param name="AdminUserId">The optional administrator identifier associated with the run.</param>
public sealed record WorkflowObjectExecutionRequest(
    string ObjectId,
    string Input,
    int AiModelId,
    string CorrelationId,
    IReadOnlyDictionary<string, string>? Parameters = null,
    int? AdminUserId = null);

/// <summary>
/// Defines stable parameter keys understood by Workflow object providers.
/// </summary>
public static class WorkflowObjectExecutionParameters
{
    /// <summary>Allows the provider to invoke registered function calls.</summary>
    public const string AllowFunctionCalls = "allowFunctionCalls";
    /// <summary>Sets the permitted human-in-the-loop level.</summary>
    public const string HumanInTheLoopLevel = "humanInTheLoopLevel";
    /// <summary>Sets plugin-tool permission behavior.</summary>
    public const string PluginToolPermission = "pluginToolPermission";
    /// <summary>Sets MCP-tool permission behavior.</summary>
    public const string McpToolPermission = "mcpToolPermission";
    /// <summary>Includes the human participant in the execution context.</summary>
    public const string IncludeHumanParticipant = "includeHumanParticipant";
    /// <summary>Sets the maximum chat round count.</summary>
    public const string ChatMaxRound = "chatMaxRound";
    /// <summary>Provides the selected Agent personality text.</summary>
    public const string Personality = "personality";
}

/// <summary>
/// Identifies a task, chat group, or provider object produced by an object execution.
/// </summary>
/// <param name="Kind">The reference kind.</param>
/// <param name="ProviderId">The provider that owns the reference.</param>
/// <param name="ChatTaskId">The related chat-task identifier, when available.</param>
/// <param name="ChatGroupId">The related chat-group identifier, when available.</param>
/// <param name="DisplayName">The display name of the referenced object.</param>
/// <param name="ObjectId">The provider-local object identifier.</param>
public sealed record WorkflowObjectExecutionReference(
    string Kind,
    string ProviderId,
    int? ChatTaskId = null,
    int? ChatGroupId = null,
    string? DisplayName = null,
    string? ObjectId = null);

/// <summary>
/// Reports the output or failure produced by a Workflow object provider.
/// </summary>
/// <param name="Success">Whether execution completed successfully.</param>
/// <param name="Output">The provider output when execution succeeds.</param>
/// <param name="ErrorMessage">The failure message when execution fails.</param>
public sealed record WorkflowObjectExecutionResult(
    bool Success,
    string? Output,
    string? ErrorMessage = null)
{
    /// <summary>
    /// Gets or initializes the optional reference to a task or provider object created by execution.
    /// </summary>
    public WorkflowObjectExecutionReference? Reference { get; init; }
}

/// <summary>
/// Defines the provider boundary used to enumerate and execute Workflow objects.
/// </summary>
public interface IWorkflowObjectProvider
{
    /// <summary>
    /// Gets the stable provider identifier used in Workflow object references.
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Lists objects currently available from the provider.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel discovery.</param>
    /// <returns>The provider objects available for Workflow selection.</returns>
    ValueTask<IReadOnlyList<WorkflowObjectDescriptor>> GetObjectsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes one provider object with the supplied Workflow context.
    /// </summary>
    /// <param name="request">The object identifier, input, model, and execution parameters.</param>
    /// <param name="cancellationToken">Token used to cancel execution.</param>
    /// <returns>The provider execution result and optional reference.</returns>
    ValueTask<WorkflowObjectExecutionResult> ExecuteAsync(
        WorkflowObjectExecutionRequest request,
        CancellationToken cancellationToken = default);
}
