/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentWorkflowReferenceContracts.cs
    File description: AgentWorkflowReferenceContracts.cs related implementation

    Created by: Senparc - 20260822

    Modified by: Senparc - 20260822
    Change description: v0.3.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.3.2 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.3.2.2 Improve source documentation, headers, and module metadata consistency.

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
    /// <summary>
    /// Validates Agent and Group references before a Workflow graph is persisted.
    /// </summary>
    /// <param name="workflowId">The Workflow identifier being saved.</param>
    /// <param name="adminUserId">The administrator performing the save.</param>
    /// <param name="references">The Agent and Group references declared by the graph.</param>
    /// <param name="cancellationToken">Token used to cancel validation.</param>
    /// <returns>
    /// <see langword="null"/> when all references are valid; otherwise a user-facing validation message.
    /// </returns>
    Task<string?> ValidateWorkflowReferencesAsync(
        int workflowId,
        int adminUserId,
        IReadOnlyCollection<AgentWorkflowReference> references,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates Workflow bindings before an Agent configuration is persisted.
    /// </summary>
    /// <param name="agentId">The Agent identifier being saved.</param>
    /// <param name="adminUserId">The administrator performing the save.</param>
    /// <param name="workflowIds">The Workflow identifiers bound to the Agent.</param>
    /// <param name="cancellationToken">Token used to cancel validation.</param>
    /// <returns>
    /// <see langword="null"/> when all bindings are valid; otherwise a user-facing validation message.
    /// </returns>
    Task<string?> ValidateAgentBindingsAsync(
        int agentId,
        int adminUserId,
        IReadOnlyCollection<int> workflowIds,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Identifies an Agent or Group reference embedded in a Workflow graph.
/// </summary>
/// <param name="Kind">The reference kind, such as <c>agent</c> or <c>group</c>.</param>
/// <param name="Id">The referenced persistence identifier.</param>
public sealed record AgentWorkflowReference(string Kind, int Id);
