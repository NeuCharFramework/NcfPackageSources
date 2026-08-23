/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AgentWorkflowReferenceContracts.cs
    文件功能描述：AgentWorkflowReferenceContracts.cs 相关实现


    创建标识：Senparc - 20260822

    修改标识：Senparc - 20260822
    修改描述：v0.3.0 新增 AgentsManager 与工作流之间的引用校验契约

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Abstractions;

/// <summary>
/// Cross-module guard used by NeuChar Workflow before a graph is saved.
/// AgentsManager owns Agent and Group persistence, while Workflow owns its graph.
/// </summary>
public interface IAgentWorkflowReferenceValidator
{
    Task<string?> ValidateWorkflowReferencesAsync(
        int workflowId,
        int adminUserId,
        IReadOnlyCollection<AgentWorkflowReference> references,
        CancellationToken cancellationToken = default);

    Task<string?> ValidateAgentBindingsAsync(
        int agentId,
        int adminUserId,
        IReadOnlyCollection<int> workflowIds,
        CancellationToken cancellationToken = default);
}

public sealed record AgentWorkflowReference(string Kind, int Id);
