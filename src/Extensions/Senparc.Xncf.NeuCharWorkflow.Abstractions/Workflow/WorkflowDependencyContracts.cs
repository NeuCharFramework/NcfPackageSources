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
