/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowDependencyProvider.cs
    File description: NeuCharWorkflowDependencyProvider.cs related implementation

    Created by: Senparc - 20260822

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance workflow function calls, task control, and replay management

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.5.0.0 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WorkflowEntity = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>
/// Exposes only the reference edges needed for cross-module cycle checks.
/// Prompt text, secrets and node parameters never leave the Workflow module.
/// </summary>
public sealed class NeuCharWorkflowDependencyProvider : IWorkflowDependencyProvider
{
    private readonly NeuCharWorkflowService _workflowService;

    /// <summary>
    /// Initializes the dependency provider with the workflow persistence service.
    /// </summary>
    /// <param name="workflowService">The service used to load owner-scoped workflows.</param>
    public NeuCharWorkflowDependencyProvider(NeuCharWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    /// <summary>
    /// Returns the cross-module dependency edges for an owner-scoped workflow.
    /// Only identifiers and dependency kinds are exposed; graph content and parameters remain private.
    /// </summary>
    /// <param name="workflowId">The workflow identifier to inspect.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="cancellationToken">The cancellation token for the lookup.</param>
    /// <returns>A dependency snapshot, or <see langword="null"/> when the identifiers are invalid or the workflow is not owned by the administrator.</returns>
    public async Task<WorkflowDependencySnapshot?> GetSnapshotAsync(
        int workflowId,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (workflowId <= 0 || adminUserId <= 0)
        {
            return null;
        }

        var workflow = await _workflowService.GetObjectAsync(item =>
                item.Id == workflowId && item.AdminUserId == adminUserId)
            .ConfigureAwait(false);
        if (workflow == null)
        {
            return null;
        }

        return new WorkflowDependencySnapshot(
            workflow.Id,
            workflow.Name,
            workflow.Enabled,
            ExtractReferences(workflow));
    }

    /// <summary>
    /// Parses a workflow graph and extracts distinct sub-workflow, agent, and group references.
    /// Malformed graph JSON is treated as having no references.
    /// </summary>
    /// <param name="workflow">The workflow whose graph is inspected.</param>
    /// <returns>A distinct list of dependency references.</returns>
    private static IReadOnlyList<WorkflowDependencyReference> ExtractReferences(
        WorkflowEntity workflow)
    {
        try
        {
            var graph = JsonSerializer.Deserialize<NeuCharWorkflowGraph>(
                string.IsNullOrWhiteSpace(workflow.GraphJson) ? "{}" : workflow.GraphJson);
            var references = new List<WorkflowDependencyReference>();

            foreach (var node in graph?.Nodes ?? new List<NeuCharWorkflowNode>())
            {
                var config = node.Config;
                if (string.Equals(node.Type, "sub-workflow", StringComparison.OrdinalIgnoreCase))
                {
                    AddReference(references, "workflow", ReadInt(config, "workflowId"));
                    continue;
                }

                if (!string.Equals(node.Type, "agent", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(node.Type, "agent-group", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var providerId = ReadString(config, "providerId");
                var objectId = ReadString(config, "objectId");
                if (!string.Equals(providerId, "agents-manager", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (objectId.StartsWith("agent:", StringComparison.OrdinalIgnoreCase))
                {
                    AddReference(references, "agent", ParseSuffix(objectId, "agent:"));
                }
                else if (objectId.StartsWith("group:", StringComparison.OrdinalIgnoreCase))
                {
                    AddReference(references, "group", ParseSuffix(objectId, "group:"));
                }
            }

            return references
                .GroupBy(reference => $"{reference.Kind}:{reference.Id}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<WorkflowDependencyReference>();
        }
    }

    /// <summary>
    /// Adds a positive dependency identifier to the collection.
    /// </summary>
    /// <param name="references">The destination reference collection.</param>
    /// <param name="kind">The dependency kind.</param>
    /// <param name="id">The referenced identifier.</param>
    private static void AddReference(
        ICollection<WorkflowDependencyReference> references,
        string kind,
        int id)
    {
        if (id > 0)
        {
            references.Add(new WorkflowDependencyReference(kind, id));
        }
    }

    /// <summary>
    /// Reads an integer-valued graph property, returning zero for absent or invalid values.
    /// </summary>
    /// <param name="config">The node configuration object.</param>
    /// <param name="key">The property name.</param>
    /// <returns>The parsed integer or zero.</returns>
    private static int ReadInt(
        System.Text.Json.Nodes.JsonObject config,
        string key)
        => int.TryParse(ReadString(config, key), out var value) ? value : 0;

    /// <summary>
    /// Reads a graph configuration property as text.
    /// </summary>
    /// <param name="config">The node configuration object.</param>
    /// <param name="key">The property name.</param>
    /// <returns>The property text or an empty string when absent.</returns>
    private static string ReadString(
        System.Text.Json.Nodes.JsonObject config,
        string key)
        => config?.TryGetPropertyValue(key, out var value) == true
            ? value?.ToString() ?? string.Empty
            : string.Empty;

    /// <summary>
    /// Parses the numeric suffix after a known reference prefix.
    /// </summary>
    /// <param name="value">The complete reference value.</param>
    /// <param name="prefix">The prefix whose length is removed before parsing.</param>
    /// <returns>The parsed identifier or zero when parsing fails.</returns>
    private static int ParseSuffix(string value, string prefix)
        => int.TryParse(value[prefix.Length..], out var id) ? id : 0;
}
