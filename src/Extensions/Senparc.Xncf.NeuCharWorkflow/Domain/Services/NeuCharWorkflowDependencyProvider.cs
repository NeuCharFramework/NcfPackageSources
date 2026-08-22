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

    public NeuCharWorkflowDependencyProvider(NeuCharWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

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

    private static int ReadInt(
        System.Text.Json.Nodes.JsonObject config,
        string key)
        => int.TryParse(ReadString(config, key), out var value) ? value : 0;

    private static string ReadString(
        System.Text.Json.Nodes.JsonObject config,
        string key)
        => config?.TryGetPropertyValue(key, out var value) == true
            ? value?.ToString() ?? string.Empty
            : string.Empty;

    private static int ParseSuffix(string value, string prefix)
        => int.TryParse(value[prefix.Length..], out var id) ? id : 0;
}
