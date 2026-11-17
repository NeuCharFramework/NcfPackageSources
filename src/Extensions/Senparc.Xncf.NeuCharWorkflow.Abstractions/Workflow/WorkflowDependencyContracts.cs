/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkflowDependencyContracts.cs
    File description: WorkflowDependencyContracts.cs related implementation

    Created by: Senparc - 20260822

    Modified by: Senparc - 20260822
    Change description: v0.2.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;

/// <summary>
/// Read-only dependency view used by other modules to detect cross-module cycles.
/// </summary>
public interface IWorkflowDependencyProvider
{
    Task<WorkflowDependencySnapshot?> GetSnapshotAsync(
        int workflowId,
        int adminUserId,
        CancellationToken cancellationToken = default);
}

public sealed record WorkflowDependencySnapshot(
    int Id,
    string Name,
    bool Enabled,
    IReadOnlyList<WorkflowDependencyReference> References);

public sealed record WorkflowDependencyReference(string Kind, int Id);
