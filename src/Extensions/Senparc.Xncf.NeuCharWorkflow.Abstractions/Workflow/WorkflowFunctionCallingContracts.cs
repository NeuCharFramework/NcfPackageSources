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
