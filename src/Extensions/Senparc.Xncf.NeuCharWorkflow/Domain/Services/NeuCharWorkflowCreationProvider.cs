/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowCreationProvider.cs
    File description: Controlled AI-assisted Workflow creation boundary

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.Xncf.NeuCharWorkflow.Application.AppServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>
/// Provides a deliberately narrow AI creation boundary. The normal Workflow editor
/// remains the authority for unrestricted administrative editing.
/// </summary>
public sealed class NeuCharWorkflowCreationProvider : IWorkflowCreationProvider
{
    private const int MaxAiNodes = 30;
    private const int MaxAiEdges = 60;
    private static readonly HashSet<string> AllowedNodeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "manual-trigger", "function", "delay", "condition", "aggregate", "merge",
        "parallel", "loop", "loop-end", "console", "neubell", "end"
    };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly NeuCharWorkflowAppService _workflowAppService;
    private readonly NeuCharWorkflowFunctionService _functionService;
    private readonly NeuCharWorkflowEngine _workflowEngine;
    private readonly XncfModuleService _moduleService;

    public NeuCharWorkflowCreationProvider(
        NeuCharWorkflowAppService workflowAppService,
        NeuCharWorkflowFunctionService functionService,
        NeuCharWorkflowEngine workflowEngine,
        XncfModuleService moduleService)
    {
        _workflowAppService = workflowAppService;
        _functionService = functionService;
        _workflowEngine = workflowEngine;
        _moduleService = moduleService;
    }

    public async Task<WorkflowCreationCapabilities> GetCapabilitiesAsync(
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (adminUserId <= 0 || !await IsModuleOpenAsync().ConfigureAwait(false))
        {
            return new WorkflowCreationCapabilities(
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<WorkflowCreationFunctionDescriptor>(),
                MaxAiNodes,
                MaxAiEdges);
        }

        var functions = (await _functionService.GetCatalogAsync(
                null,
                loadParameterOptions: false,
                cancellationToken)
            .ConfigureAwait(false))
            .Where(function => function.ModuleAvailable && function.AllowAiInvocation)
            .Select(function => new WorkflowCreationFunctionDescriptor(
                function.ModuleUid,
                function.ModuleName,
                function.FunctionKey,
                function.Name,
                Limit(function.Description, 500),
                function.ModuleAvailable))
            .Take(200)
            .ToList();

        return new WorkflowCreationCapabilities(
            new[] { "manual" },
            AllowedNodeTypes.OrderBy(type => type, StringComparer.OrdinalIgnoreCase).ToList(),
            functions,
            MaxAiNodes,
            MaxAiEdges,
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger", "name": "Manual trigger", "config": {} },
                { "id": "console", "type": "console", "name": "Print current time", "config": { "printTemplate": "Current time: {{= formatDate(now(), \"yyyy-MM-dd HH:mm:ss\") }}" } },
                { "id": "neubell", "type": "neubell", "name": "Send NeuBell", "config": { "title": "Workflow completed", "summary": "{{input}}", "consumeMode": "item" } },
                { "id": "end", "type": "end", "name": "End", "config": {} }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "console" },
                { "id": "edge-2", "source": "console", "target": "neubell" },
                { "id": "edge-3", "source": "neubell", "target": "end" }
              ]
            }
            """);
    }

    public async Task<WorkflowCreationValidationResult> ValidateAsync(
        int adminUserId,
        WorkflowCreationRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (adminUserId <= 0)
        {
            return Failure("not-authorized", Localize("AiCreation.NotAuthorized", "The current administrator is not available."));
        }

        if (!await IsModuleOpenAsync().ConfigureAwait(false))
        {
            return Failure("module-unavailable", Localize("AiCreation.ModuleUnavailable", "NeuChar Workflow is not enabled."));
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            return Failure("invalid-request", Localize("AiCreation.NameRequired", "A Workflow name is required."));
        }

        if (request.Name.Trim().Length > 200 || request.Description?.Length > 10_000)
        {
            return Failure(
                "metadata-too-large",
                Localize("AiCreation.MetadataTooLarge", "The Workflow name or description exceeds the allowed size."));
        }

        if (!string.Equals(request.TriggerType?.Trim(), "manual", StringComparison.OrdinalIgnoreCase))
        {
            return Failure("trigger-not-allowed", Localize("AiCreation.TriggerNotAllowed", "AI-created Workflows currently support only a manual trigger."));
        }

        if (string.IsNullOrWhiteSpace(request.GraphJson) || request.GraphJson.Length > 1_000_000)
        {
            return Failure("graph-too-large", Localize("AiCreation.GraphTooLarge", "The Workflow graph is empty or exceeds the allowed size."));
        }

        if (ContainsSensitiveProperty(request.GraphJson))
        {
            return Failure("sensitive-config", Localize("AiCreation.SensitiveConfig", "Sensitive credentials and external-resume secrets cannot be supplied by AI Workflow creation."));
        }

        NeuCharWorkflowGraph graph;
        try
        {
            graph = _workflowEngine.ParseAndValidateGraph(
                request.GraphJson,
                requireAllNodesReachable: false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException or NullReferenceException)
        {
            return Failure("invalid-graph", ex.Message);
        }

        if (graph.Nodes.Count > MaxAiNodes || graph.Edges.Count > MaxAiEdges)
        {
            return Failure(
                "graph-limit",
                Localize("AiCreation.GraphLimit", $"AI-created Workflows are limited to {MaxAiNodes} nodes and {MaxAiEdges} connections."));
        }

        var triggerNodes = graph.Nodes
            .Where(node => node.Type?.EndsWith("trigger", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();
        if (triggerNodes.Count != 1 ||
            !string.Equals(triggerNodes[0].Type, "manual-trigger", StringComparison.OrdinalIgnoreCase))
        {
            return Failure("manual-trigger-required", Localize("AiCreation.ManualTriggerRequired", "The graph must contain exactly one manual trigger."));
        }

        if (graph.Nodes.Count(node => string.Equals(node.Type, "end", StringComparison.OrdinalIgnoreCase)) != 1)
        {
            return Failure("end-required", Localize("AiCreation.EndRequired", "The graph must contain exactly one end node."));
        }

        foreach (var node in graph.Nodes)
        {
            if (!AllowedNodeTypes.Contains(node.Type))
            {
                return Failure(
                    "node-not-allowed",
                    Localize("AiCreation.NodeNotAllowed", $"The node type '{node.Type}' is not allowed for AI-created Workflows."));
            }

            if (string.Equals(node.Type, "function", StringComparison.OrdinalIgnoreCase))
            {
                var functionError = await ValidateFunctionNodeAsync(node, cancellationToken).ConfigureAwait(false);
                if (functionError != null)
                {
                    return Failure("function-not-allowed", functionError);
                }
            }
        }

        var disconnected = _workflowEngine.GetDisconnectedNodes(graph);
        if (disconnected.Count > 0)
        {
            return Failure(
                "disconnected-node",
                Localize("AiCreation.DisconnectedNode", "Every AI-created Workflow node must be connected to the manual trigger."));
        }

        var referenceError = await _workflowEngine
            .ValidateReferencesAsync(graph, cancellationToken)
            .ConfigureAwait(false);
        if (referenceError != null)
        {
            return Failure("invalid-reference", referenceError);
        }

        return new WorkflowCreationValidationResult(
            true,
            JsonSerializer.Serialize(graph, JsonOptions),
            graph.Nodes.Count,
            graph.Edges.Count,
            Array.Empty<string>());
    }

    public async Task<WorkflowCreationResult> CreateAsync(
        int adminUserId,
        WorkflowCreationRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(adminUserId, request, cancellationToken).ConfigureAwait(false);
        if (!validation.Success)
        {
            return new WorkflowCreationResult(
                false,
                0,
                request?.Name,
                request?.TriggerType,
                false,
                validation.ErrorCode,
                validation.ErrorMessage);
        }

        try
        {
            var detail = await _workflowAppService.SaveAsync(
                new SaveWorkflowCommand(
                    0,
                    request.Name,
                    request.Description,
                    validation.NormalizedGraphJson,
                    request.Enabled,
                    "manual",
                    "{}",
                    SaveSource: "manual"),
                adminUserId,
                cancellationToken).ConfigureAwait(false);

            return new WorkflowCreationResult(
                true,
                detail.Id,
                detail.Name,
                detail.TriggerType,
                detail.Enabled);
        }
        catch (Exception ex)
        {
            return new WorkflowCreationResult(
                false,
                0,
                request.Name,
                "manual",
                false,
                "create-failed",
                ex.Message);
        }
    }

    private async Task<string?> ValidateFunctionNodeAsync(
        NeuCharWorkflowNode node,
        CancellationToken cancellationToken)
    {
        var moduleUid = GetString(node.Config, "moduleUid");
        var functionKey = GetString(node.Config, "functionKey");
        if (string.IsNullOrWhiteSpace(moduleUid) || string.IsNullOrWhiteSpace(functionKey))
        {
            return Localize("AiCreation.FunctionReferenceRequired", "A Function node must specify moduleUid and functionKey.");
        }

        var descriptor = await _functionService
            .GetFunctionAsync(moduleUid, functionKey, loadParameterOptions: true, cancellationToken)
            .ConfigureAwait(false);
        if (descriptor == null)
        {
            return Localize("AiCreation.FunctionNotFound", "The requested Function does not exist.");
        }

        if (!descriptor.ModuleAvailable)
        {
            return Localize("AiCreation.FunctionModuleUnavailable", "The Function module is not enabled.");
        }

        if (!descriptor.AllowAiInvocation)
        {
            return Localize("AiCreation.FunctionAiDenied", "The requested Function is not available for AI invocation.");
        }

        return null;
    }

    private async Task<bool> IsModuleOpenAsync()
    {
        var module = await _moduleService
            .GetObjectAsync(module => module.Uid == new Register().Uid)
            .ConfigureAwait(false);
        return module?.State == XncfModules_State.开放;
    }

    private static bool ContainsSensitiveProperty(string json)
    {
        try
        {
            var node = JsonNode.Parse(json);
            return ContainsSensitiveProperty(node);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool ContainsSensitiveProperty(JsonNode? node)
    {
        if (node is JsonObject objectNode)
        {
            foreach (var property in objectNode)
            {
                var key = property.Key.Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
                if (key.Contains("password", StringComparison.Ordinal)
                    || key.Contains("secret", StringComparison.Ordinal)
                    || key.Contains("token", StringComparison.Ordinal)
                    || key.Contains("apikey", StringComparison.Ordinal)
                    || key.Contains("externalresume", StringComparison.Ordinal))
                {
                    return true;
                }

                if (ContainsSensitiveProperty(property.Value))
                {
                    return true;
                }
            }
        }
        else if (node is JsonArray arrayNode)
        {
            return arrayNode.Any(ContainsSensitiveProperty);
        }

        return false;
    }

    private static string? GetString(JsonObject? config, string key)
    {
        if (config == null || !config.TryGetPropertyValue(key, out var value) || value == null)
        {
            return null;
        }

        return value.GetValueKind() == JsonValueKind.String
            ? value.GetValue<string>()
            : value.ToJsonString();
    }

    private static WorkflowCreationValidationResult Failure(string code, string message) =>
        new(false, null, 0, 0, Array.Empty<string>(), code, message);

    private static string Localize(string key, string fallback) =>
        NeuCharWorkflowResource.Get(key, fallback);

    private static string Limit(string? value, int max) =>
        string.IsNullOrEmpty(value) || value.Length <= max ? value ?? string.Empty : value[..max];
}
