/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkflowFunctionCallingContracts.cs
    File description: WorkflowFunctionCallingContracts.cs related implementation

    Created by: Senparc - 20260821

    Modified by: Senparc - 20260822
    Change description: v0.2.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;

/// <summary>
/// Optional integration boundary used by hosts that want to expose enabled Workflows
/// as AI function-calling tools.
/// </summary>
public interface IWorkflowFunctionCallingProvider
{
    Task<IReadOnlyList<WorkflowFunctionCallingDescriptor>> GetAvailableAsync(
        int adminUserId,
        CancellationToken cancellationToken = default);

    Task<WorkflowFunctionCallingResult> ExecuteAsync(
        int workflowId,
        int adminUserId,
        string input,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);
}

public sealed record WorkflowFunctionCallingDescriptor(
    int Id,
    string Name,
    string? Description,
    IReadOnlyList<WorkflowFunctionCallingParameter> Parameters);

/// <summary>
/// Workflow trigger parameters are intentionally optional in function-calling.
/// The normal <c>input</c> argument remains the stable required entry point.
/// </summary>
public sealed record WorkflowFunctionCallingParameter(
    string Name,
    string? Description);

public sealed record WorkflowFunctionCallingResult(
    bool Success,
    string? Output,
    string? ErrorMessage);

/// <summary>
/// Optional, explicitly gated integration boundary used by hosts that want to let
/// an AI assistant design and create a Workflow for the current administrator.
/// Implementations must validate the definition again at save time.
/// </summary>
public interface IWorkflowCreationProvider
{
    Task<WorkflowCreationCapabilities> GetCapabilitiesAsync(
        int adminUserId,
        CancellationToken cancellationToken = default);

    Task<WorkflowCreationValidationResult> ValidateAsync(
        int adminUserId,
        WorkflowCreationRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkflowCreationResult> CreateAsync(
        int adminUserId,
        WorkflowCreationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record WorkflowCreationRequest(
    string Name,
    string? Description,
    string TriggerType,
    string GraphJson,
    bool Enabled = true);

public sealed record WorkflowCreationCapabilities(
    IReadOnlyList<string> TriggerTypes,
    IReadOnlyList<string> NodeTypes,
    IReadOnlyList<WorkflowCreationFunctionDescriptor> FunctionRenders,
    int MaxNodes,
    int MaxEdges,
    string? ExampleGraphJson = null);

public sealed record WorkflowCreationFunctionDescriptor(
    string ModuleUid,
    string ModuleName,
    string FunctionKey,
    string Name,
    string Description,
    bool ModuleAvailable);

public sealed record WorkflowCreationValidationResult(
    bool Success,
    string? NormalizedGraphJson,
    int NodeCount,
    int EdgeCount,
    IReadOnlyList<string> Warnings,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record WorkflowCreationResult(
    bool Success,
    int WorkflowId,
    string? Name,
    string? TriggerType,
    bool Enabled,
    string? ErrorCode = null,
    string? ErrorMessage = null);
