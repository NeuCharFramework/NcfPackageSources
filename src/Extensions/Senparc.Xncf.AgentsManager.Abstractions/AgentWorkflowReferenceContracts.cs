/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: AgentWorkflowReferenceContracts.cs
    File description: AgentWorkflowReferenceContracts.cs related implementation


    Created by: Senparc - 20260822

    Modified by: Senparc - 20260822
    Change description: v0.3.0 Updated module behavior and related support.

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
