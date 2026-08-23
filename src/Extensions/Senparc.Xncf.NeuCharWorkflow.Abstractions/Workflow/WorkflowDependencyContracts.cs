/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkflowDependencyContracts.cs
    文件功能描述：WorkflowDependencyContracts.cs 相关实现


    创建标识：Senparc - 20260822

    修改标识：Senparc - 20260822
    修改描述：v0.2.0 扩展工作流依赖、函数调用和人工交互契约

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
