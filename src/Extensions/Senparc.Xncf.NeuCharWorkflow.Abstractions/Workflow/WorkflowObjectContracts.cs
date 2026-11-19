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

----------------------------------------------------------------*/

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;

/// <summary>
///Legacy comment translated to English.
/// </summary>
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

public sealed record WorkflowObjectExecutionRequest(
    string ObjectId,
    string Input,
    int AiModelId,
    string CorrelationId,
    IReadOnlyDictionary<string, string>? Parameters = null,
    int? AdminUserId = null);

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public static class WorkflowObjectExecutionParameters
{
    public const string AllowFunctionCalls = "allowFunctionCalls";
    public const string HumanInTheLoopLevel = "humanInTheLoopLevel";
    public const string PluginToolPermission = "pluginToolPermission";
    public const string McpToolPermission = "mcpToolPermission";
    public const string IncludeHumanParticipant = "includeHumanParticipant";
    public const string ChatMaxRound = "chatMaxRound";
    public const string Personality = "personality";
}

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public sealed record WorkflowObjectExecutionReference(
    string Kind,
    string ProviderId,
    int? ChatTaskId = null,
    int? ChatGroupId = null,
    string? DisplayName = null,
    string? ObjectId = null);

public sealed record WorkflowObjectExecutionResult(
    bool Success,
    string? Output,
    string? ErrorMessage = null)
{
    ///Legacy comment translated to English.
    public WorkflowObjectExecutionReference? Reference { get; init; }
}

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public interface IWorkflowObjectProvider
{
    string ProviderId { get; }

    ValueTask<IReadOnlyList<WorkflowObjectDescriptor>> GetObjectsAsync(
        CancellationToken cancellationToken = default);

    ValueTask<WorkflowObjectExecutionResult> ExecuteAsync(
        WorkflowObjectExecutionRequest request,
        CancellationToken cancellationToken = default);
}
