/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowAppService.cs
    File description: enhance workflow orchestration, replay, Webhook, and parallel execution

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20260815
    Change description: v0.2.0 enhance workflow parallelism and execution control

    Modified by: Senparc - 20260817
    Change description: v0.2.0 support pausing and external resumption of Human Input nodes

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance workflow function calls, task control, and replay management

    Modified by: Senparc - 20260829
    Change description: v0.3.0 add workflow analysis queries and admin-side visualization

    Modified by: Senparc - 20260909
    Change description: v0.4.0 add the Chat trigger: chat sessions, message sending, and run-status queries

    Modified by: Senparc - 20260913
    Change description: v0.4.0 persist Chat history in the database and support recovery after cross-host restarts

    Modified by: Senparc - 20260915
    Change description: v0.4.0 enhance Chat trigger message persistence and recovery

    Modified by: Senparc - 20261021
    Change description: v0.4.4 provide workflow import and export APIs

    Modified by: Senparc - 20261028
    Change description: vNext capture the request tenant snapshot at runtime startup and restore tenant context in the execution scope

    Modified by: Senparc - 20261103
    Change description: v0.4.4 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.5.0.0 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Senparc.Xncf.AgentsManager.Abstractions;
using Senparc.Xncf.NeuCharWorkflow.Application.Events;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel;
using Senparc.Xncf.NeuCharWorkflow.Domain.Services;
using WorkflowEntity = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Application.AppServices;

/// <summary>
/// Application-service boundary for workflow design, import/export, execution,
/// task recovery, analytics, webhook triggers, and chat sessions.
/// </summary>
public sealed class NeuCharWorkflowAppService
{
    private const int TaskListPageSize = 30;
    private static readonly TimeSpan PersistedRunRecoveryAge = TimeSpan.FromMinutes(10);
    private static readonly JsonSerializerOptions DesignerJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly NeuCharWorkflowService _workflowService;
    private readonly NeuCharWorkflowVersionService _workflowVersionService;
    private readonly NeuCharWorkflowExecutionLogService _executionLogService;
    private readonly NeuCharWorkflowEngine _workflowEngine;
    private readonly NeuCharWorkflowFunctionService _functionService;
    private readonly NeuCharWorkflowAnalyticsService _analyticsService;
    private readonly NeuCharWorkflowRunCoordinator _runCoordinator;
    private readonly WorkflowEventPublisher _eventPublisher;
    private readonly XncfModuleService _xncfModuleService;
    private readonly IWorkflowHumanInteractionBridge _humanInteractionBridge;
    private readonly NeuCharWorkflowHumanInputService _humanInputService;
    private readonly NeuCharWorkflowChatSessionService _chatSessionService;
    private readonly NeuCharWorkflowChatMessageService _chatMessageService;
    private readonly IAgentWorkflowReferenceValidator? _agentWorkflowReferenceValidator;
    private readonly ILogger<NeuCharWorkflowAppService> _logger;
    private readonly IServiceProvider? _serviceProvider;

    /// <summary>
    /// Initializes the workflow application service and its design/runtime collaborators.
    /// </summary>
    /// <param name="workflowService">Workflow metadata persistence service.</param>
    /// <param name="workflowVersionService">Workflow version persistence service.</param>
    /// <param name="executionLogService">Execution log and replay persistence service.</param>
    /// <param name="workflowEngine">Graph validation and execution engine.</param>
    /// <param name="functionService">Workflow function catalog service.</param>
    /// <param name="analyticsService">Workflow analytics service.</param>
    /// <param name="runCoordinator">In-process run ownership and status coordinator.</param>
    /// <param name="eventPublisher">Workflow event publisher.</param>
    /// <param name="xncfModuleService">XNCF module availability service.</param>
    /// <param name="humanInteractionBridge">Bridge for AgentManager human interactions.</param>
    /// <param name="humanInputService">Native workflow human-input service.</param>
    /// <param name="chatSessionService">In-memory workflow chat session service.</param>
    /// <param name="chatMessageService">Persisted workflow chat message service.</param>
    /// <param name="agentWorkflowReferenceValidators">Optional validators for AgentManager references.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="serviceProvider">Optional provider used to capture tenant context.</param>
    public NeuCharWorkflowAppService(
        NeuCharWorkflowService workflowService,
        NeuCharWorkflowVersionService workflowVersionService,
        NeuCharWorkflowExecutionLogService executionLogService,
        NeuCharWorkflowEngine workflowEngine,
        NeuCharWorkflowFunctionService functionService,
        NeuCharWorkflowAnalyticsService analyticsService,
        NeuCharWorkflowRunCoordinator runCoordinator,
        WorkflowEventPublisher eventPublisher,
        XncfModuleService xncfModuleService,
        IWorkflowHumanInteractionBridge humanInteractionBridge,
        NeuCharWorkflowHumanInputService humanInputService,
        NeuCharWorkflowChatSessionService chatSessionService,
        NeuCharWorkflowChatMessageService chatMessageService,
        IEnumerable<IAgentWorkflowReferenceValidator>? agentWorkflowReferenceValidators = null,
        ILogger<NeuCharWorkflowAppService>? logger = null,
        IServiceProvider? serviceProvider = null)
    {
        _workflowService = workflowService;
        _workflowVersionService = workflowVersionService;
        _executionLogService = executionLogService;
        _workflowEngine = workflowEngine;
        _functionService = functionService;
        _analyticsService = analyticsService;
        _runCoordinator = runCoordinator;
        _eventPublisher = eventPublisher;
        _xncfModuleService = xncfModuleService;
        _humanInteractionBridge = humanInteractionBridge;
        _humanInputService = humanInputService;
        _chatSessionService = chatSessionService;
        _chatMessageService = chatMessageService;
        _logger = logger ?? NullLogger<NeuCharWorkflowAppService>.Instance;
        _agentWorkflowReferenceValidator = agentWorkflowReferenceValidators?.FirstOrDefault();
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Lists workflows owned by an administrator, newest updates first.
    /// </summary>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>A task containing workflow list items.</returns>
    public async Task<IReadOnlyList<WorkflowListItem>> GetListAsync(int adminUserId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workflows = await _workflowService.GetFullListAsync(
            z => z.AdminUserId == adminUserId,
            z => z.LastUpdateTime,
            OrderingType.Descending).ConfigureAwait(false);
        return workflows.Select(ToListItem).ToList();
    }

    /// <summary>
    /// Loads an owned workflow with an editable graph, running count, and observed output schemas.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="cancellationToken">Token used to cancel graph and runtime queries.</param>
    /// <returns>The workflow detail, or <see langword="null"/> when it is not owned by the administrator.</returns>
    public async Task<WorkflowDetail?> GetDetailAsync(int workflowId, int adminUserId, CancellationToken cancellationToken = default)
    {
        var workflow = await GetOwnedWorkflowAsync(workflowId, adminUserId).ConfigureAwait(false);
        if (workflow == null)
        {
            return null;
        }
        var editableGraphTask = _workflowEngine.BuildEditableGraphJsonAsync(
            workflow.GraphJson,
            cancellationToken);
        var runningCountTask = GetRunningCountAsync(
            adminUserId,
            workflow.Id,
            cancellationToken);
        await Task.WhenAll(editableGraphTask, runningCountTask).ConfigureAwait(false);
        return ToDetail(
            workflow,
            editableGraphTask.Result,
            runningCount: runningCountTask.Result,
            observedOutputSchemas: Array.Empty<NeuCharWorkflowObservedOutputSchema>());
    }

    /// <summary>
    /// Reads observed output schemas from recent completed runs for an owned workflow.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="cancellationToken">Token used to cancel log reads.</param>
    /// <returns>Observed schemas, or an empty list when the workflow is not owned.</returns>
    public async Task<IReadOnlyList<NeuCharWorkflowObservedOutputSchema>> GetObservedOutputSchemasAsync(
        int workflowId,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (await GetOwnedWorkflowAsync(workflowId, adminUserId).ConfigureAwait(false) == null)
        {
            return Array.Empty<NeuCharWorkflowObservedOutputSchema>();
        }

        return await ReadObservedOutputSchemasAsync(workflowId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the designer catalog of available workflow functions and object references.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel catalog loading.</param>
    /// <returns>Designer function metadata and workflow object metadata.</returns>
    public async Task<WorkflowDesignerData> GetDesignerDataAsync(CancellationToken cancellationToken = default)
    {
        var catalog = await _functionService.GetCatalogAsync(null, true, cancellationToken).ConfigureAwait(false);
        var objects = await _workflowEngine.GetWorkflowObjectsAsync(cancellationToken).ConfigureAwait(false);
        var functions = catalog.Select(descriptor =>
        {
            var parameterSchema = WorkflowFunctionSchemaBuilder.Build(descriptor);
            return new WorkflowDesignerFunction(
                descriptor.FunctionKey,
                descriptor.Name,
                descriptor.Description,
                descriptor.ModuleUid,
                descriptor.ModuleName,
                descriptor.ModuleVersion,
                descriptor.ModuleAvailable,
                descriptor.ModuleAvailable ? "open" : "disabled",
                JsonSerializer.Serialize(parameterSchema, DesignerJsonOptions),
                JsonSerializer.Serialize(WorkflowFunctionSchemaBuilder.BuildDefaults(parameterSchema), DesignerJsonOptions),
                descriptor.Output,
                descriptor.CatalogError);
        }).ToList();
        return new WorkflowDesignerData(functions, objects);
    }

    /// <summary>
    /// Exports an owned workflow with editable graph data and redacted secret properties.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="format">The JSON or YAML interchange format.</param>
    /// <param name="cancellationToken">Token used to cancel export processing.</param>
    /// <returns>The file name, content type, and serialized workflow content.</returns>
    /// <exception cref="WorkflowNotFoundException">Thrown when the workflow is not owned by the administrator.</exception>
    /// <exception cref="WorkflowInputException">Thrown when stored graph data cannot be parsed.</exception>
    public async Task<WorkflowInterchangeExport> ExportAsync(
        int workflowId,
        int adminUserId,
        WorkflowInterchangeFormat format,
        CancellationToken cancellationToken = default)
    {
        var workflow = await GetOwnedWorkflowAsync(workflowId, adminUserId).ConfigureAwait(false)
            ?? throw new WorkflowNotFoundException();
        var editableGraphJson = await _workflowEngine
            .BuildEditableGraphJsonAsync(workflow.GraphJson, cancellationToken)
            .ConfigureAwait(false);
        var graph = JsonSerializer.Deserialize<NeuCharWorkflowGraph>(
            editableGraphJson,
            DesignerJsonOptions) ?? throw new WorkflowInputException("工作流图数据无效。");
        var definition = new NeuCharWorkflowDefinition
        {
            Metadata = new WorkflowDefinitionMetadata
            {
                Name = workflow.Name,
                Description = workflow.Description
            },
            Trigger = new WorkflowDefinitionTrigger
            {
                Type = workflow.TriggerType,
                Config = ParseJsonObject(workflow.TriggerConfigJson)
            },
            Graph = graph
        };

        RedactExportSecrets(definition);
        var extension = format == WorkflowInterchangeFormat.Json ? "workflow.json" : "workflow.yaml";
        return new WorkflowInterchangeExport(
            $"{SanitizeFileName(workflow.Name)}.{extension}",
            format == WorkflowInterchangeFormat.Json ? "application/json; charset=utf-8" : "application/yaml; charset=utf-8",
            WorkflowInterchangeCodec.Serialize(definition, format));
    }

    /// <summary>
    /// Parses and validates workflow interchange content without persisting it.
    /// </summary>
    /// <param name="content">The JSON or YAML workflow content.</param>
    /// <param name="format">Optional explicit interchange format.</param>
    /// <param name="cancellationToken">Token used to cancel validation before parsing.</param>
    /// <returns>A task containing validation errors and the parsed definition when valid.</returns>
    public Task<WorkflowImportPreview> ValidateImportAsync(
        string content,
        WorkflowInterchangeFormat? format = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateImportPreview(content, format));
    }

    /// <summary>
    /// Validates and persists an imported workflow as a disabled draft owned by the administrator.
    /// </summary>
    /// <param name="content">The JSON or YAML workflow content.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="format">Optional explicit interchange format.</param>
    /// <param name="cancellationToken">Token used to cancel validation or persistence.</param>
    /// <returns>The saved workflow detail.</returns>
    /// <exception cref="WorkflowInputException">Thrown when import validation fails.</exception>
    public async Task<WorkflowDetail> ImportAsync(
        string content,
        int adminUserId,
        WorkflowInterchangeFormat? format = null,
        CancellationToken cancellationToken = default)
    {
        var preview = CreateImportPreview(content, format);
        if (!preview.Valid || preview.Definition == null)
        {
            throw new WorkflowInputException(string.Join(
                Environment.NewLine,
                preview.Errors.Select(issue => issue.Message)));
        }

        var definition = preview.Definition;
        return await SaveAsync(
            new SaveWorkflowCommand(
                0,
                string.IsNullOrWhiteSpace(definition.Metadata.Name) ? "Imported Workflow" : definition.Metadata.Name,
                definition.Metadata.Description,
                JsonSerializer.Serialize(definition.Graph, DesignerJsonOptions),
                false,
                definition.Trigger.Type,
                definition.Trigger.Config.ToJsonString(DesignerJsonOptions),
                SaveSource: "import"),
            adminUserId,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Parses interchange content, validates its graph and trigger consistency,
    /// and returns a non-persisting import preview.
    /// </summary>
    /// <param name="content">The JSON or YAML interchange content.</param>
    /// <param name="format">An optional explicit format.</param>
    /// <returns>The validation preview and parsed definition when valid.</returns>
    private WorkflowImportPreview CreateImportPreview(
        string content,
        WorkflowInterchangeFormat? format)
    {
        var errors = new List<WorkflowImportIssue>();
        try
        {
            var document = WorkflowInterchangeCodec.Parse(content, format);
            var definition = document.Definition;
            _workflowEngine.ParseAndValidateGraph(
                JsonSerializer.Serialize(definition.Graph, DesignerJsonOptions),
                requireAllNodesReachable: false);
            var expectedTrigger = $"{definition.Trigger.Type}-trigger";
            if (!definition.Graph.Nodes.Any(node =>
                    string.Equals(node.Type, expectedTrigger, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add(new WorkflowImportIssue(
                    "error",
                    "trigger-mismatch",
                    "Workflow 触发器配置与图中的触发器节点不一致。",
                    "trigger.type"));
            }

            return new WorkflowImportPreview(
                errors.Count == 0,
                string.IsNullOrWhiteSpace(definition.Metadata.Name) ? "Imported Workflow" : definition.Metadata.Name,
                definition.Trigger.Type,
                errors,
                Array.Empty<WorkflowImportIssue>(),
                definition.Secrets,
                definition);
        }
        catch (WorkflowInterchangeException ex)
        {
            errors.Add(new WorkflowImportIssue("error", "invalid-document", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(new WorkflowImportIssue("error", "invalid-graph", ex.Message, "graph"));
        }

        return new WorkflowImportPreview(
            false,
            "Imported Workflow",
            "manual",
            errors,
            Array.Empty<WorkflowImportIssue>(),
            Array.Empty<WorkflowSecretRequirement>());
    }

    /// <summary>
    /// Parses a JSON object or returns an empty object for blank input.
    /// </summary>
    /// <param name="json">The JSON object text.</param>
    /// <returns>The parsed object.</returns>
    /// <exception cref="WorkflowInputException">Thrown when the value is not a JSON object.</exception>
    private static JsonObject ParseJsonObject(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new JsonObject();
        }

        return JsonNode.Parse(json) as JsonObject
            ?? throw new WorkflowInputException("Workflow 触发器配置无效。");
    }

    /// <summary>
    /// Removes export-sensitive trigger secrets and records the required secret paths
    /// in the interchange definition.
    /// </summary>
    /// <param name="definition">The export definition to redact.</param>
    private static void RedactExportSecrets(NeuCharWorkflowDefinition definition)
    {
        if (definition.Trigger.Type == "webhook")
        {
            definition.Trigger.Config["token"] = string.Empty;
            definition.Secrets.Add(new WorkflowSecretRequirement
            {
                Path = "trigger.config.token",
                Reason = "Webhook token must be configured after import."
            });
        }

        foreach (var node in definition.Graph.Nodes.Where(node => node.Config != null))
        {
            RedactSecretProperties(node.Config!, $"graph.nodes[{node.Id}].config", definition.Secrets);
        }
    }

    /// <summary>
    /// Recursively replaces configured secret properties in a JSON value with empty strings.
    /// </summary>
    /// <param name="value">The JSON value to redact.</param>
    /// <param name="secretNames">Property names treated as secrets.</param>
    private static void RedactSecretProperties(
        JsonObject config,
        string path,
        ICollection<WorkflowSecretRequirement> secrets)
    {
        foreach (var property in config.ToList())
        {
            if (property.Key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                property.Key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                property.Key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                property.Key.Contains("apikey", StringComparison.OrdinalIgnoreCase) ||
                property.Key.Contains("api_key", StringComparison.OrdinalIgnoreCase) ||
                property.Key.Equals("externalResumeKey", StringComparison.OrdinalIgnoreCase))
            {
                config[property.Key] = string.Empty;
                secrets.Add(new WorkflowSecretRequirement
                {
                    Path = $"{path}.{property.Key}",
                    Reason = "Sensitive value must be configured after import."
                });
            }
            else if (property.Value is JsonObject child)
            {
                RedactSecretProperties(child, $"{path}.{property.Key}", secrets);
            }
        }
    }

    /// <summary>
    /// Converts a workflow name into a safe interchange file-name component.
    /// </summary>
    /// <param name="name">The workflow name.</param>
    /// <returns>A bounded file-name component.</returns>
    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(character => invalid.Contains(character) ? '_' : character).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "workflow" : sanitized;
    }

    /// <summary>
    /// Validates, protects, normalizes, and persists a workflow graph with optimistic revision checks.
    /// </summary>
    /// <param name="request">The workflow name, graph, trigger, enablement, and revision command.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="cancellationToken">Token used to cancel validation or persistence.</param>
    /// <returns>The saved workflow detail.</returns>
    /// <exception cref="WorkflowInputException">Thrown when graph, references, trigger, secret, or request validation fails.</exception>
    /// <exception cref="WorkflowConflictException">Thrown when the expected revision is stale.</exception>
    /// <exception cref="WorkflowNotFoundException">Thrown when an update targets a workflow not owned by the administrator.</exception>
    public async Task<WorkflowDetail> SaveAsync(
        SaveWorkflowCommand request,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureModuleEnabledAsync().ConfigureAwait(false);
        ValidateSaveCommand(request);
        NeuCharWorkflowGraph graph;
        try
        {
            graph = _workflowEngine.ParseAndValidateGraph(request.GraphJson, requireAllNodesReachable: false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException)
        {
            throw new WorkflowInputException(ex.Message, ex);
        }

        var workflow = request.Id > 0
            ? await GetOwnedWorkflowAsync(request.Id, adminUserId).ConfigureAwait(false)
            : null;
        if (request.Id > 0 && workflow == null)
        {
            throw new WorkflowNotFoundException();
        }
        if (workflow != null && request.ExpectedRevision.HasValue &&
            request.ExpectedRevision.Value != workflow.Revision)
        {
            throw new WorkflowConflictException("工作流已被其他页面更新，请刷新后再保存。");
        }

        try
        {
            await _workflowEngine.MergeExistingSecretsAsync(graph, workflow?.GraphJson, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            throw new WorkflowInputException(ex.Message, ex);
        }

        var hasDisconnectedNodes = _workflowEngine.GetDisconnectedNodes(graph).Count > 0;
        //Legacy comment translated to English.
        var enabled = request.Enabled && !hasDisconnectedNodes;
        var subWorkflowError = await _workflowEngine.ValidateSubWorkflowReferencesAsync(
            graph,
            workflow?.Id ?? 0,
            adminUserId,
            requireEnabled: enabled,
            cancellationToken).ConfigureAwait(false);
        if (subWorkflowError != null)
        {
            throw new WorkflowInputException(subWorkflowError);
        }

        if (_agentWorkflowReferenceValidator != null && workflow?.Id > 0)
        {
            var agentReferences = GetAgentWorkflowReferences(graph);
            var agentReferenceError = await _agentWorkflowReferenceValidator
                .ValidateWorkflowReferencesAsync(
                    workflow.Id,
                    adminUserId,
                    agentReferences,
                    cancellationToken)
                .ConfigureAwait(false);
            if (agentReferenceError != null)
            {
                throw new WorkflowInputException(agentReferenceError);
            }
        }
        if (enabled)
        {
            var referenceError = await _workflowEngine.ValidateReferencesAsync(graph, cancellationToken).ConfigureAwait(false);
            if (referenceError != null)
            {
                throw new WorkflowInputException(referenceError);
            }
        }

        var triggerType = request.TriggerType?.Trim().ToLowerInvariant() switch
        {
            "interval" => "interval",
            "webhook" => "webhook",
            "chat" => "chat",
            "manual" or null or "" => "manual",
            _ => null
        };
        if (triggerType == null)
        {
            throw new WorkflowInputException("工作流触发方式无效。");
        }
        if (!graph.Nodes.Any(z => string.Equals(z.Type, $"{triggerType}-trigger", StringComparison.OrdinalIgnoreCase)))
        {
            throw new WorkflowInputException("工作流触发器节点与触发方式不一致。");
        }

        try
        {
            await _workflowEngine.ProtectSecretsAsync(graph, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            throw new WorkflowInputException(ex.Message, ex);
        }

        var normalizedGraphJson = JsonSerializer.Serialize(graph, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        workflow ??= new WorkflowEntity(request.Name.Trim(), adminUserId);
        string triggerConfigJson;
        try
        {
            triggerConfigJson = triggerType switch
            {
                "webhook" => NeuCharWorkflowWebhookConfig.Normalize(
                    request.TriggerConfigJson,
                    workflow.TriggerConfigJson).ToJson(),
                "chat" => NeuCharWorkflowChatConfig.Normalize(
                    request.TriggerConfigJson,
                    workflow.TriggerConfigJson).ToJson(),
                "interval" => request.TriggerConfigJson ?? "{}",
                _ => "{}"
            };
        }
        catch (InvalidOperationException ex)
        {
            throw new WorkflowInputException(ex.Message, ex);
        }

        var nextRun = enabled
            ? NeuCharWorkflowEngine.CalculateNextRun(triggerType, triggerConfigJson, DateTime.UtcNow)
            : null;
        var autoSaveMinutes = request.AutoSaveMinutes <= 0 ? 0 : Math.Clamp(request.AutoSaveMinutes, 1, 1440);
        if (workflow.Id > 0 && IsUnchanged(workflow, request, normalizedGraphJson, enabled, triggerType, triggerConfigJson, autoSaveMinutes))
        {
            var editableUnchanged = await _workflowEngine.BuildEditableGraphJsonAsync(workflow.GraphJson, cancellationToken)
                .ConfigureAwait(false);
            return ToDetail(
                workflow,
                editableUnchanged,
                unchanged: true,
                runningCount: await GetRunningCountAsync(
                    adminUserId,
                    workflow.Id,
                    cancellationToken).ConfigureAwait(false));
        }

        workflow.Update(request.Name, request.Description, normalizedGraphJson, enabled, triggerType,
            triggerConfigJson, nextRun, autoSaveMinutes);
        await _workflowService.SaveObjectAsync(workflow).ConfigureAwait(false);
        await SaveVersionAsync(workflow, adminUserId, request.SaveSource).ConfigureAwait(false);
        await _eventPublisher.PublishAsync(workflow.Id, "saved", adminUserId, cancellationToken).ConfigureAwait(false);
        var editableGraphJson = await _workflowEngine.BuildEditableGraphJsonAsync(workflow.GraphJson, cancellationToken)
            .ConfigureAwait(false);
        return ToDetail(
            workflow,
            editableGraphJson,
            runningCount: await GetRunningCountAsync(
                adminUserId,
                workflow.Id,
                cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Validates and executes an owned workflow synchronously in the current request.
    /// Runtime start and completion state are persisted around engine execution.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="input">The workflow input text or JSON.</param>
    /// <param name="cancellationToken">Token used to cancel execution.</param>
    /// <param name="parseInputAsJson">Whether the input should be parsed as a JSON value.</param>
    /// <returns>The workflow execution result.</returns>
    /// <exception cref="WorkflowInputException">Thrown when input or graph validation fails.</exception>
    /// <exception cref="WorkflowNotFoundException">Thrown when the workflow is not owned by the administrator.</exception>
    public async Task<NeuCharWorkflowRunResult> RunImmediatelyAsync(
        int workflowId,
        int adminUserId,
        string input,
        CancellationToken cancellationToken = default,
        bool parseInputAsJson = false)
    {
        await EnsureModuleEnabledAsync().ConfigureAwait(false);
        ValidateRunInput(workflowId, input);
        var workflow = await GetOwnedWorkflowAsync(workflowId, adminUserId).ConfigureAwait(false)
            ?? throw new WorkflowNotFoundException();
        await ValidateWorkflowAsync(workflow, cancellationToken).ConfigureAwait(false);
        workflow.MarkStarted(workflow.NextRunAt);
        await _workflowService.SaveRuntimeStartedAsync(workflow).ConfigureAwait(false);
        var result = await _workflowEngine.RunAsync(
            workflow,
            input,
            cancellationToken,
            parseInputAsJson: parseInputAsJson).ConfigureAwait(false);
        workflow.MarkCompleted(result.Success, result.ErrorMessage);
        await _workflowService.SaveRuntimeCompletedAsync(workflow).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Validates that an owned workflow can run with the supplied input without starting execution.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="input">The input text to validate.</param>
    /// <param name="cancellationToken">Token used to cancel validation.</param>
    /// <exception cref="WorkflowInputException">Thrown when input, graph, or reference validation fails.</exception>
    public async Task ValidateRunAsync(int workflowId, int adminUserId, string input, CancellationToken cancellationToken = default)
    {
        await EnsureModuleEnabledAsync().ConfigureAwait(false);
        ValidateRunInput(workflowId, input);
        var workflow = await GetOwnedWorkflowAsync(workflowId, adminUserId).ConfigureAwait(false)
            ?? throw new WorkflowNotFoundException();
        await ValidateWorkflowAsync(workflow, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates and schedules an owned workflow for coordinated background execution.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="input">The workflow input text.</param>
    /// <param name="cancellationToken">Token used to cancel validation.</param>
    /// <returns>The newly assigned run identifier.</returns>
    /// <exception cref="WorkflowConflictException">Thrown when another run owns the workflow or scheduling fails.</exception>
    public async Task<Guid> StartRunAsync(int workflowId, int adminUserId, string input, CancellationToken cancellationToken = default)
    {
        await ValidateRunAsync(workflowId, adminUserId, input, cancellationToken).ConfigureAwait(false);
        if (!_runCoordinator.TryStart(workflowId, adminUserId, input, out var runId, out var error, "manual", RequestTenantSnapshot.Capture(_serviceProvider)))
        {
            throw new WorkflowConflictException(error);
        }
        return runId;
    }

    /// <summary>
    /// Returns a run snapshot and pending human interactions visible to the administrator.
    /// </summary>
    /// <param name="runId">The run identifier.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="afterSequence">Only return events after this sequence.</param>
    /// <param name="cancellationToken">Token used to query external human interactions.</param>
    /// <returns>The snapshot, or <see langword="null"/> when unavailable or unauthorized.</returns>
    public async Task<NeuCharWorkflowRunSnapshot?> GetRunStatusAsync(
        Guid runId,
        int adminUserId,
        long afterSequence,
        CancellationToken cancellationToken = default)
    {
        if (runId == Guid.Empty)
        {
            return null;
        }

        var snapshot = _runCoordinator.GetSnapshot(runId, adminUserId, afterSequence);
        if (snapshot == null)
        {
            return null;
        }

        var correlationId = BuildRunCorrelationId(snapshot.WorkflowId, runId);
        var nativeInteractions = _humanInputService.GetPendingInteractions(
            correlationId,
            adminUserId.ToString());
        var agentInteractions = await _humanInteractionBridge.GetPendingAsync(
            correlationId,
            adminUserId.ToString(),
            cancellationToken).ConfigureAwait(false);
        var interactions = nativeInteractions
            .Concat(agentInteractions)
            .OrderBy(interaction => interaction.CreatedAt)
            .ToList();
        return snapshot with { HumanInteractions = interactions };
    }

    /// <summary>
    /// Returns a synchronous run snapshot without querying external human-interaction providers.
    /// </summary>
    /// <param name="runId">The run identifier.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="afterSequence">Only return events after this sequence.</param>
    /// <returns>The snapshot, or <see langword="null"/> when unavailable.</returns>
    public NeuCharWorkflowRunSnapshot? GetRunStatus(Guid runId, int adminUserId, long afterSequence) =>
        runId == Guid.Empty ? null : _runCoordinator.GetSnapshot(runId, adminUserId, afterSequence);

    /// <summary>
    /// Resolves a pending native or AgentManager human interaction for a workflow run.
    /// </summary>
    /// <param name="runId">The workflow run identifier.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="requestId">The pending interaction identifier.</param>
    /// <param name="approved">Whether the requested operation is approved.</param>
    /// <param name="input">Optional human input text.</param>
    /// <param name="reason">Optional approval/rejection reason.</param>
    /// <param name="cancellationToken">Token used to cancel resolution.</param>
    /// <returns>The interaction resolution result.</returns>
    public async Task<WorkflowHumanInteractionResult> ResolveHumanInteractionAsync(
        Guid runId,
        int adminUserId,
        string requestId,
        bool approved,
        string? input = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (runId == Guid.Empty)
        {
            return new WorkflowHumanInteractionResult(false, false, null, null, "缺少有效的 Workflow 运行 ID。");
        }

        var snapshot = _runCoordinator.GetSnapshot(runId, adminUserId, 0);
        if (snapshot == null)
        {
            return new WorkflowHumanInteractionResult(false, false, null, null, "Workflow 运行不存在或不属于当前账号。");
        }

        var correlationId = BuildRunCorrelationId(snapshot.WorkflowId, runId);
        var nativeResolution = await _humanInputService.ResolveForAdminAsync(
            correlationId,
            adminUserId.ToString(),
            requestId,
            approved,
            input,
            reason,
            cancellationToken).ConfigureAwait(false);
        if (nativeResolution.Handled)
        {
            return new WorkflowHumanInteractionResult(
                nativeResolution.Success,
                nativeResolution.Approved,
                nativeResolution.Input,
                nativeResolution.Reason,
                nativeResolution.Message);
        }

        return await _humanInteractionBridge.ResolveAsync(
            correlationId,
            adminUserId.ToString(),
            requestId,
            approved,
            input,
            reason,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the stable correlation identifier shared by workflow runtime and interaction stores.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="runId">The run identifier.</param>
    /// <returns>A correlation identifier containing both values.</returns>
    public static string BuildRunCorrelationId(int workflowId, Guid runId)
        => $"workflow-{workflowId}-run-{runId:N}";

    /// <summary>
    /// Aborts an active coordinated run or safely closes an old persisted run after restart recovery checks.
    /// </summary>
    /// <param name="runId">Optional in-memory run identifier.</param>
    /// <param name="executionLogId">Optional persisted execution-log identifier.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="cancellationToken">Token used to cancel persistence and event publication.</param>
    /// <exception cref="WorkflowInputException">Thrown when neither run identifier is valid.</exception>
    /// <exception cref="WorkflowConflictException">Thrown when the run is missing, not owned, or too recent for recovery abort.</exception>
    public async Task AbortRunAsync(
        Guid? runId,
        int? executionLogId,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        var validRunId = runId.GetValueOrDefault();
        if (validRunId == Guid.Empty && (!executionLogId.HasValue || executionLogId.Value <= 0))
        {
            throw new WorkflowInputException("中止请求缺少有效的运行 ID。");
        }

        if (validRunId != Guid.Empty &&
            _runCoordinator.TryAbort(validRunId, adminUserId, out _))
        {
            return;
        }

        var persistedExecutionLogId = executionLogId.GetValueOrDefault();
        var executionLog = persistedExecutionLogId > 0
            ? await _executionLogService.GetUnfinishedByIdAsync(persistedExecutionLogId, cancellationToken)
                .ConfigureAwait(false)
            : await _executionLogService.GetUnfinishedByRunIdAsync(validRunId, cancellationToken)
                .ConfigureAwait(false);
        if (executionLog == null ||
            await GetOwnedWorkflowAsync(executionLog.WorkflowId, adminUserId).ConfigureAwait(false) == null)
        {
            throw new WorkflowConflictException("运行任务不存在或不属于当前账号。");
        }

        if (executionLog.StartedAt > DateTime.UtcNow.Subtract(PersistedRunRecoveryAge))
        {
            throw new WorkflowConflictException("当前进程未持有该运行；它可能仍由其他实例执行，暂不能安全中止。");
        }

        const string message = "服务重启后未能恢复此运行，已按手动中止结束。";
        executionLog.Complete(false, null, message);
        await _executionLogService.SaveObjectAsync(executionLog).ConfigureAwait(false);
        await _eventPublisher.PublishAsync(
            executionLog.WorkflowId,
            "aborted-after-restart",
            adminUserId,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns a paged list of workflow execution tasks, combining active coordinator runs
    /// with persisted logs and optional status/date/workflow filters.
    /// </summary>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="beforeExecutionLogId">Optional cursor for older persisted logs.</param>
    /// <param name="workflowId">Optional workflow filter.</param>
    /// <param name="status">Optional normalized status filter.</param>
    /// <param name="from">Optional inclusive date filter.</param>
    /// <param name="to">Optional inclusive date filter.</param>
    /// <param name="cancellationToken">Token used to cancel queries.</param>
    /// <returns>A task list page with optional analytics summary.</returns>
    public async Task<WorkflowTaskListPage> GetTaskListAsync(
        int adminUserId,
        int? beforeExecutionLogId = null,
        int? workflowId = null,
        string? status = null,
        string? from = null,
        string? to = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var statusFilter = NeuCharWorkflowAnalyticsService.NormalizeStatus(status);
        var fromUtc = NeuCharWorkflowAnalyticsService.ParseDate(from);
        var toUtc = NeuCharWorkflowAnalyticsService.ParseDate(to)?.AddDays(1);
        var workflowNames = await _workflowService.GetNameMapAsync(
            adminUserId,
            cancellationToken).ConfigureAwait(false);
        var workflowIds = workflowNames.Keys.ToList();
        if (workflowId.HasValue && workflowId.Value > 0)
        {
            if (!workflowNames.ContainsKey(workflowId.Value))
            {
                return new WorkflowTaskListPage(Array.Empty<WorkflowTaskListItem>(), false, null);
            }

            workflowIds = new List<int> { workflowId.Value };
        }
        var loadLatest = !beforeExecutionLogId.HasValue || beforeExecutionLogId.Value <= 0;
        var activeRuns = loadLatest
            ? _runCoordinator.GetActiveRuns(adminUserId)
                .Where(run => !workflowId.HasValue || workflowId.Value <= 0 || run.WorkflowId == workflowId.Value)
                .Where(run => !fromUtc.HasValue || run.StartedAt.UtcDateTime >= fromUtc.Value)
                .Where(run => !toUtc.HasValue || run.StartedAt.UtcDateTime < toUtc.Value)
                .ToArray()
            : Array.Empty<NeuCharWorkflowActiveRun>();

        var tasks = string.IsNullOrWhiteSpace(statusFilter) || statusFilter == "running"
            ? activeRuns.Select(run => new WorkflowTaskListItem(
                $"live:{run.RunId:N}",
                run.WorkflowId,
                workflowNames.TryGetValue(run.WorkflowId, out var workflowName) ? workflowName : $"工作流 #{run.WorkflowId}",
                run.Source,
                "running",
                run.StartedAt,
                null,
                BuildLiveSummary(run),
                null,
                run.RunId,
                null,
                false)).ToList()
            : new List<WorkflowTaskListItem>();

        if (workflowIds.Count == 0)
        {
            return new WorkflowTaskListPage(tasks.OrderByDescending(z => z.StartedAt).ToList(), false, null);
        }

        var logPage = await _executionLogService.GetTaskPageAsync(
            workflowIds,
            beforeExecutionLogId,
            TaskListPageSize + 1,
            statusFilter,
            fromUtc,
            toUtc,
            cancellationToken).ConfigureAwait(false);
        var activeRunIds = activeRuns.Select(z => z.RunId).ToHashSet();
        var fetchedLogs = logPage.ToList();
        var pageLogs = fetchedLogs.Take(TaskListPageSize).ToList();
        tasks.AddRange(pageLogs
            // Do not duplicate a persisted log that is still represented by an active
            // coordinator-owned run in the current process.
            .Where(log => log.FinishedAt != null ||
                          !activeRunIds.Contains(GetTaskRunId(log.WorkflowId, log.CorrelationId).GetValueOrDefault()))
            .Select(log => new WorkflowTaskListItem(
                $"log:{log.Id}",
                log.WorkflowId,
                string.IsNullOrWhiteSpace(log.WorkflowName)
                    ? (workflowNames.TryGetValue(log.WorkflowId, out var workflowName) ? workflowName : $"工作流 #{log.WorkflowId}")
                    : log.WorkflowName,
                "history",
                log.FinishedAt == null
                    ? "running"
                    : log.Succeeded == true ? "success" : "failed",
                ToUtcOffset(log.StartedAt),
                ToUtcOffset(log.FinishedAt),
                log.ResultSummary,
                log.Succeeded == false ? log.Error : null,
                GetTaskRunId(log.WorkflowId, log.CorrelationId),
                log.Id,
                log.ReplayAvailable)));

        return new WorkflowTaskListPage(
            tasks.OrderByDescending(z => z.StartedAt).ToList(),
            fetchedLogs.Count > TaskListPageSize,
            pageLogs.LastOrDefault()?.Id,
            await _analyticsService.GetSummaryAsync(
                    adminUserId,
                    workflowId,
                    from,
                    to,
                    cancellationToken)
                .ConfigureAwait(false));
    }

    /// <summary>
    /// Backward-compatible task-list overload that exposes only the owner and cursor filters.
    /// </summary>
    public Task<WorkflowTaskListPage> GetTaskListAsync(
        int adminUserId,
        int? beforeExecutionLogId,
        CancellationToken cancellationToken) =>
        GetTaskListAsync(adminUserId, beforeExecutionLogId, null, null, null, null, cancellationToken);

    /// <summary>
    /// Retrieves workflow analytics for an administrator and query range.
    /// </summary>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="query">Analytics filters and date range.</param>
    /// <param name="cancellationToken">Token used to cancel analytics queries.</param>
    /// <returns>A task containing the analytics result.</returns>
    public Task<WorkflowAnalyticsResult> GetAnalyticsAsync(
        int adminUserId,
        WorkflowAnalyticsQuery query,
        CancellationToken cancellationToken = default) =>
        _analyticsService.GetAsync(adminUserId, query, cancellationToken);

    /// <summary>
    /// Previews completed execution logs eligible for cleanup.
    /// </summary>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>A task containing cleanup counts and cutoff metadata.</returns>
    public async Task<WorkflowTaskCleanupPreview> PreviewTaskCleanupAsync(
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow;
        var logs = await GetCompletedTaskLogsAsync(adminUserId, cutoff, cancellationToken).ConfigureAwait(false);
        return ToTaskCleanupPreview(logs, cutoff);
    }

    /// <summary>
    /// Deletes completed execution logs at or before the normalized cutoff.
    /// </summary>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="cutoff">The UTC or convertible cutoff time.</param>
    /// <param name="cancellationToken">Token used to cancel querying and deletion.</param>
    /// <returns>A task containing the deleted count and effective cutoff.</returns>
    /// <exception cref="WorkflowInputException">Thrown when the cutoff is invalid.</exception>
    public async Task<WorkflowTaskCleanupResult> CleanupCompletedTasksAsync(
        int adminUserId,
        DateTime cutoff,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var effectiveCutoff = cutoff.Kind == DateTimeKind.Utc ? cutoff : cutoff.ToUniversalTime();
        if (effectiveCutoff == DateTime.MinValue)
        {
            throw new WorkflowInputException("清理请求缺少有效的预览截止时间。");
        }
        if (effectiveCutoff > DateTime.UtcNow)
        {
            effectiveCutoff = DateTime.UtcNow;
        }

        var logs = await GetCompletedTaskLogsAsync(adminUserId, effectiveCutoff, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        // The cleanup operation is intentionally performed only after the preview query
        // and cutoff normalization have completed.
        await _executionLogService.DeleteAllAsync(logs).ConfigureAwait(false);

        return new WorkflowTaskCleanupResult(logs.Count, effectiveCutoff);
    }

    /// <summary>
    /// Loads a replay definition and event sequence for a completed owned execution.
    /// </summary>
    /// <param name="executionLogId">The persisted execution-log identifier.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="cancellationToken">Token used to cancel replay loading.</param>
    /// <returns>The replay data, or <see langword="null"/> when not found or not owned.</returns>
    /// <exception cref="WorkflowConflictException">Thrown when execution is still running.</exception>
    /// <exception cref="WorkflowInputException">Thrown when replay data is unavailable or malformed.</exception>
    public async Task<WorkflowRunReplay?> GetReplayAsync(
        int executionLogId,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var executionLog = await _executionLogService.GetObjectAsync(log => log.Id == executionLogId).ConfigureAwait(false);
        if (executionLog == null || await GetOwnedWorkflowAsync(executionLog.WorkflowId, adminUserId).ConfigureAwait(false) == null)
        {
            return null;
        }
        if (executionLog.FinishedAt == null)
        {
            throw new WorkflowConflictException("该工作流仍在运行，请在运行结束后再启动回看。");
        }
        if (!CanReplay(executionLog))
        {
            throw new WorkflowInputException("该任务产生于回看功能启用前，未保存可回放的节点事件或快照。");
        }

        var snapshotLog = !string.IsNullOrWhiteSpace(executionLog.ReplaySnapshotJson)
            ? executionLog
            : await _executionLogService.GetReplaySnapshotAsync(
                executionLog.WorkflowId,
                executionLog.ReplaySnapshotHash!).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(snapshotLog?.ReplaySnapshotJson))
        {
            throw new WorkflowInputException("该任务引用的工作流快照已不可用，无法开始回看。");
        }

        NeuCharWorkflowReplayDefinition definition;
        IReadOnlyList<WorkflowReplayEvent> events;
        try
        {
            definition = JsonSerializer.Deserialize<NeuCharWorkflowReplayDefinition>(
                snapshotLog.ReplaySnapshotJson,
                DesignerJsonOptions) ?? throw new JsonException("快照为空。");
            var progress = JsonSerializer.Deserialize<List<NeuCharWorkflowProgress>>(
                executionLog.ReplayEventsJson!,
                DesignerJsonOptions) ?? new List<NeuCharWorkflowProgress>();
            events = progress.Select((item, index) => new WorkflowReplayEvent(
                    index + 1,
                    item.NodeId,
                    item.NodeName,
                    item.Status,
                    item.Message,
                    item.Output,
                    item.Timestamp,
                    item.OutputSchema,
                    item.Input)
                {
                    ObjectReference = item.ObjectReference
                })
                .ToList();
        }
        catch (JsonException ex)
        {
            throw new WorkflowInputException("该任务的回看数据已损坏，无法解析。", ex);
        }

        return new WorkflowRunReplay(
            executionLog.Id,
            executionLog.WorkflowId,
            executionLog.WorkflowName,
            ToTaskStatus(executionLog),
            ToUtcOffset(executionLog.StartedAt),
            ToUtcOffset(executionLog.FinishedAt),
            executionLog.Succeeded,
            executionLog.ResultSummary,
            executionLog.Error,
            definition,
            events);
    }

    /// <summary>
    /// Copies a completed replay snapshot into a new disabled workflow draft.
    /// </summary>
    /// <param name="executionLogId">The persisted execution-log identifier.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="cancellationToken">Token used to cancel copy persistence.</param>
    /// <returns>The copied workflow detail.</returns>
    public async Task<WorkflowDetail> CopyReplayAsDraftAsync(
        int executionLogId,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        var replay = await GetReplayAsync(executionLogId, adminUserId, cancellationToken).ConfigureAwait(false)
            ?? throw new WorkflowNotFoundException();
        var snapshot = replay.Definition;
        var graph = _workflowEngine.ParseAndValidateGraph(snapshot.GraphJson, requireAllNodesReachable: false);
        var graphJson = JsonSerializer.Serialize(graph, DesignerJsonOptions);
        var copy = new WorkflowEntity(CreateReplayCopyName(snapshot.Name), adminUserId);
        copy.Update(
            copy.Name,
            snapshot.Description,
            graphJson,
            enabled: false,
            triggerType: snapshot.TriggerType,
            triggerConfigJson: snapshot.TriggerConfigJson,
            nextRunAt: null,
            autoSaveMinutes: snapshot.AutoSaveMinutes);
        await _workflowService.SaveObjectAsync(copy).ConfigureAwait(false);
        await SaveVersionAsync(copy, adminUserId, "replay-copy").ConfigureAwait(false);
        await _eventPublisher.PublishAsync(copy.Id, "created-from-replay", adminUserId, cancellationToken).ConfigureAwait(false);
        var editableGraphJson = await _workflowEngine.BuildEditableGraphJsonAsync(copy.GraphJson, cancellationToken)
            .ConfigureAwait(false);
        return ToDetail(copy, editableGraphJson);
    }

    /// <summary>
    /// Deletes an owned workflow, its versions, execution logs, chat messages, and metadata.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="cancellationToken">Token used to cancel event publication.</param>
    /// <exception cref="WorkflowNotFoundException">Thrown when the workflow is not owned by the administrator.</exception>
    public async Task DeleteAsync(int workflowId, int adminUserId, CancellationToken cancellationToken = default)
    {
        var workflow = await GetOwnedWorkflowAsync(workflowId, adminUserId).ConfigureAwait(false)
            ?? throw new WorkflowNotFoundException();
        var versions = await _workflowVersionService.GetFullListAsync(z => z.WorkflowId == workflow.Id).ConfigureAwait(false);
        foreach (var version in versions)
        {
            await _workflowVersionService.DeleteObjectAsync(version).ConfigureAwait(false);
        }
        var logs = await _executionLogService.GetFullListAsync(z => z.WorkflowId == workflow.Id).ConfigureAwait(false);
        foreach (var log in logs)
        {
            await _executionLogService.DeleteObjectAsync(log).ConfigureAwait(false);
        }
        await SafeDeleteChatMessagesByWorkflowAsync(workflow.Id).ConfigureAwait(false);
        await _workflowService.DeleteObjectAsync(workflow).ConfigureAwait(false);
        await _eventPublisher.PublishAsync(workflow.Id, "deleted", adminUserId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates a webhook method/token and schedules an enabled webhook workflow run.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="method">The HTTP method supplied by the webhook caller.</param>
    /// <param name="suppliedToken">The caller-supplied webhook token.</param>
    /// <param name="values">Webhook parameter values.</param>
    /// <param name="cancellationToken">Token used to cancel scheduling.</param>
    /// <returns>A structured HTTP-style trigger result.</returns>
    public async Task<WorkflowWebhookTriggerResult> TriggerWebhookAsync(
        int workflowId,
        string method,
        string suppliedToken,
        IReadOnlyDictionary<string, object> values,
        CancellationToken cancellationToken = default)
    {
        if (!await IsModuleEnabledAsync().ConfigureAwait(false))
        {
            return WorkflowWebhookTriggerResult.Conflict("NeuChar Workflow 模块未安装或未开启。");
        }
        var workflow = await _workflowService.GetObjectAsync(z => z.Id == workflowId).ConfigureAwait(false);
        if (workflow == null)
        {
            return WorkflowWebhookTriggerResult.NotFound();
        }
        if (!workflow.Enabled || !string.Equals(workflow.TriggerType, "webhook", StringComparison.OrdinalIgnoreCase))
        {
            return WorkflowWebhookTriggerResult.Conflict("工作流未启用 Webhook 触发。");
        }

        NeuCharWorkflowWebhookConfig config;
        try
        {
            config = NeuCharWorkflowWebhookConfig.ParseStored(workflow.TriggerConfigJson);
        }
        catch (InvalidOperationException)
        {
            return WorkflowWebhookTriggerResult.ServerError("Webhook 配置无效，请在 Workflow 页面重新保存。");
        }
        if (!config.IsMethodAllowed(method))
        {
            return WorkflowWebhookTriggerResult.MethodNotAllowed(config.Method);
        }
        if (!TokensEqual(config.Token, suppliedToken))
        {
            return WorkflowWebhookTriggerResult.Unauthorized();
        }

        var selectedValues = config.Parameters.Count == 0
            ? values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)
            : values.Where(pair => config.Parameters.Any(parameter =>
                    string.Equals(parameter.Name, pair.Key, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var missing = config.Parameters
            .Where(parameter => parameter.Required &&
                (!selectedValues.TryGetValue(parameter.Name, out var value) || IsEmpty(value)))
            .Select(parameter => parameter.Name)
            .ToArray();
        if (missing.Length > 0)
        {
            return WorkflowWebhookTriggerResult.BadRequest($"缺少必填 Webhook 参数：{string.Join("、", missing)}。", missing);
        }

        var input = JsonSerializer.Serialize(selectedValues, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (!_runCoordinator.TryStart(workflow.Id, workflow.AdminUserId, input, out var runId, out var error, "webhook", RequestTenantSnapshot.Capture(_serviceProvider)))
        {
            return WorkflowWebhookTriggerResult.Conflict(error);
        }
        return WorkflowWebhookTriggerResult.Accepted(workflow.Id, runId);
    }

    /// <summary>
    /// Loads the chat workflow bootstrap data, restoring persisted history when memory is empty.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="participantKey">The validated user or guest participant key.</param>
    /// <param name="cancellationToken">Token used to cancel access and history queries.</param>
    /// <returns>A structured chat bootstrap result.</returns>
    public async Task<WorkflowChatBootstrapResult> GetChatBootstrapAsync(
        int workflowId,
        string participantKey,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidParticipantKey(participantKey))
        {
            return WorkflowChatBootstrapResult.BadRequest("会话标识无效。");
        }
        var access = await GetChatAccessAsync(workflowId, participantKey, cancellationToken).ConfigureAwait(false);
        if (!access.Ok)
        {
            return WorkflowChatBootstrapResult.From(access);
        }
        var workflow = access.Workflow!;
        var config = access.Config!;
        IReadOnlyList<WorkflowChatMessage> messages = _chatSessionService.GetMessages(workflowId, participantKey)
            .Select(z => new WorkflowChatMessage(z.Role, z.Content, z.Timestamp))
            .ToList();
        if (messages.Count == 0)
        {
            //Legacy comment translated to English.
            messages = await RestoreChatHistoryAsync(workflowId, participantKey, cancellationToken).ConfigureAwait(false);
        }
        var greeting = string.IsNullOrWhiteSpace(config.Greeting)
            ? $"你好！我是「{workflow.Name}」，发送消息即可启动工作流。"
            : config.Greeting;
        return WorkflowChatBootstrapResult.Ok(
            workflowId,
            config.Title ?? workflow.Name,
            greeting,
            access.IsGuest,
            _chatSessionService.HasPendingRun(workflowId, participantKey),
            _chatSessionService.GetPendingRun(workflowId, participantKey),
            messages);
    }

    /// <summary>
    /// Persists a chat user message and schedules a chat-triggered workflow run.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="participantKey">The validated user or guest participant key.</param>
    /// <param name="message">The trimmed user message, limited to 8,000 characters.</param>
    /// <param name="cancellationToken">Token used to cancel scheduling and persistence.</param>
    /// <returns>A structured chat send result.</returns>
    public async Task<WorkflowChatSendResult> SendChatMessageAsync(
        int workflowId,
        string participantKey,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidParticipantKey(participantKey))
        {
            return WorkflowChatSendResult.BadRequest("会话标识无效。");
        }
        var normalizedMessage = message?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedMessage))
        {
            return WorkflowChatSendResult.BadRequest("消息内容不能为空。");
        }
        if (normalizedMessage.Length > 8_000)
        {
            return WorkflowChatSendResult.BadRequest("消息内容不能超过 8000 个字符。");
        }
        var access = await GetChatAccessAsync(workflowId, participantKey, cancellationToken).ConfigureAwait(false);
        if (!access.Ok)
        {
            return WorkflowChatSendResult.From(access);
        }
        if (_chatSessionService.HasPendingRun(workflowId, participantKey))
        {
            return WorkflowChatSendResult.Conflict("上一条消息还在处理中，请等待完成后再发送。");
        }
        var workflow = access.Workflow!;
        _chatSessionService.AddMessage(workflowId, participantKey, NeuCharWorkflowChatSessionService.RoleUser, normalizedMessage);
        if (!_runCoordinator.TryStart(workflow.Id, workflow.AdminUserId, normalizedMessage, out var runId, out var error, "chat", RequestTenantSnapshot.Capture(_serviceProvider)))
        {
            return WorkflowChatSendResult.Conflict(error);
        }
        _chatSessionService.SetPendingRun(workflowId, participantKey, runId);
        await SafePersistChatMessageAsync(workflowId, participantKey,
            NeuCharWorkflowChatSessionService.RoleUser, normalizedMessage, runId, cancellationToken).ConfigureAwait(false);
        return WorkflowChatSendResult.Accepted(workflowId, runId);
    }

    /// <summary>
    /// Returns the status and newly completed output for a pending chat workflow run.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="participantKey">The validated user or guest participant key.</param>
    /// <param name="runId">The pending run identifier.</param>
    /// <param name="afterSequence">Only return events after this sequence.</param>
    /// <param name="cancellationToken">Token used to cancel history persistence.</param>
    /// <returns>A structured chat run result.</returns>
    public async Task<WorkflowChatRunResult> GetChatRunStatusAsync(
        int workflowId,
        string participantKey,
        Guid runId,
        long afterSequence,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidParticipantKey(participantKey))
        {
            return WorkflowChatRunResult.BadRequest("会话标识无效。");
        }
        if (runId == Guid.Empty)
        {
            return WorkflowChatRunResult.BadRequest("运行标识无效。");
        }
        var access = await GetChatAccessAsync(workflowId, participantKey, cancellationToken).ConfigureAwait(false);
        if (!access.Ok)
        {
            return WorkflowChatRunResult.From(access);
        }
        if (!_chatSessionService.HasPendingRunMatching(workflowId, participantKey, runId))
        {
            return WorkflowChatRunResult.NotFound();
        }
        var workflow = access.Workflow!;
        var snapshot = _runCoordinator.GetSnapshot(runId, workflow.AdminUserId, afterSequence);
        if (snapshot == null)
        {
            //Legacy comment translated to English.
            var lostMessage = "运行状态已丢失，请重新发送消息。";
            _chatSessionService.AddMessage(workflowId, participantKey, NeuCharWorkflowChatSessionService.RoleError, lostMessage);
            _chatSessionService.ClearPendingRun(workflowId, participantKey, runId);
            await SafePersistChatMessageAsync(workflowId, participantKey,
                NeuCharWorkflowChatSessionService.RoleError, lostMessage, runId, cancellationToken).ConfigureAwait(false);
            return WorkflowChatRunResult.Failed(workflowId, runId, lostMessage);
        }
        if (snapshot.Running)
        {
            var last = snapshot.Events.LastOrDefault();
            return WorkflowChatRunResult.InProgress(
                workflowId,
                runId,
                last?.Message ?? string.Empty,
                last?.Sequence ?? 0);
        }
        var succeeded = snapshot.Succeeded == true;
        if (succeeded)
        {
            _chatSessionService.AddMessage(
                workflowId,
                participantKey,
                NeuCharWorkflowChatSessionService.RoleAssistant,
                NormalizeChatOutput(snapshot.FinalOutput));
            await SafePersistChatMessageAsync(workflowId, participantKey,
                NeuCharWorkflowChatSessionService.RoleAssistant, NormalizeChatOutput(snapshot.FinalOutput), runId, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            var errorText = string.IsNullOrWhiteSpace(snapshot.ErrorMessage)
                ? "工作流执行失败。"
                : snapshot.ErrorMessage.Trim();
            _chatSessionService.AddMessage(workflowId, participantKey, NeuCharWorkflowChatSessionService.RoleError, errorText);
            await SafePersistChatMessageAsync(workflowId, participantKey,
                NeuCharWorkflowChatSessionService.RoleError, errorText, runId, cancellationToken).ConfigureAwait(false);
        }
        _chatSessionService.ClearPendingRun(workflowId, participantKey, runId);
        return succeeded
            ? WorkflowChatRunResult.Succeeded(workflowId, runId, NormalizeChatOutput(snapshot.FinalOutput))
            : WorkflowChatRunResult.Failed(workflowId, runId, snapshot.ErrorMessage);
    }

    /// <summary>
    /// Clears in-memory and persisted chat history for an accessible workflow session.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="participantKey">The validated user or guest participant key.</param>
    /// <param name="cancellationToken">Token used to cancel deletion.</param>
    /// <returns>A structured reset result.</returns>
    public async Task<WorkflowChatResult> ResetChatSessionAsync(
        int workflowId,
        string participantKey,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidParticipantKey(participantKey))
        {
            return WorkflowChatResult.BadRequest("会话标识无效。");
        }
        var access = await GetChatAccessAsync(workflowId, participantKey, cancellationToken).ConfigureAwait(false);
        if (!access.Ok)
        {
            return WorkflowChatResult.From(access);
        }
        _chatSessionService.Reset(workflowId, participantKey);
        await SafeDeleteChatMessagesByParticipantAsync(workflowId, participantKey, cancellationToken).ConfigureAwait(false);
        return WorkflowChatResult.Ok();
    }

    /// <summary>
    /// Resolves chat access, module availability, trigger configuration, and guest policy
    /// for one workflow participant.
    /// </summary>
    private async Task<WorkflowChatAccess> GetChatAccessAsync(
        int workflowId,
        string participantKey,
        CancellationToken cancellationToken)
    {
        if (!await IsModuleEnabledAsync().ConfigureAwait(false))
        {
            return WorkflowChatAccess.Conflict("NeuChar Workflow 模块未安装或未开启。");
        }
        var workflow = await _workflowService.GetObjectAsync(z => z.Id == workflowId).ConfigureAwait(false);
        if (workflow == null)
        {
            return WorkflowChatAccess.NotFound();
        }
        if (!workflow.Enabled || !string.Equals(workflow.TriggerType, "chat", StringComparison.OrdinalIgnoreCase))
        {
            return WorkflowChatAccess.Conflict("工作流未启用 Chat 触发。");
        }
        NeuCharWorkflowChatConfig config;
        try
        {
            config = NeuCharWorkflowChatConfig.ParseStored(workflow.TriggerConfigJson);
        }
        catch (InvalidOperationException)
        {
            return WorkflowChatAccess.ServerError("Chat 配置无效，请在 Workflow 页面重新保存。");
        }
        var isGuest = participantKey.StartsWith("guest:", StringComparison.Ordinal);
        if (isGuest && !config.AllowGuest)
        {
            return WorkflowChatAccess.Unauthorized();
        }
        return WorkflowChatAccess.Success(workflow, config, isGuest);
    }

    /// <summary>
    /// Restores persisted chat messages into the in-memory session cache.
    /// </summary>
    private async Task<IReadOnlyList<WorkflowChatMessage>> RestoreChatHistoryAsync(
        int workflowId,
        string participantKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var persisted = await _chatMessageService
                .GetHistoryAsync(workflowId, NeuCharWorkflowChatSessionService.HashParticipant(participantKey), cancellationToken)
                .ConfigureAwait(false);
            if (persisted.Count == 0)
            {
                return new List<WorkflowChatMessage>();
            }
            _chatSessionService.SeedMessages(
                workflowId,
                participantKey,
                persisted
                    .Select(z => new WorkflowChatSessionMessage(z.Role, z.Content, z.AddTime.ToUniversalTime()))
                    .ToList());
            return _chatSessionService.GetMessages(workflowId, participantKey)
                .Select(z => new WorkflowChatMessage(z.Role, z.Content, z.Timestamp))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "恢复 NeuChar Workflow Chat 历史失败：WorkflowId={WorkflowId}。", workflowId);
            return new List<WorkflowChatMessage>();
        }
    }

    /// <summary>
    /// Persists a chat message while logging and suppressing non-fatal persistence failures.
    /// </summary>
    /// <param name="message">The chat message to persist.</param>
    private async Task SafePersistChatMessageAsync(
        int workflowId,
        string participantKey,
        string role,
        string content,
        Guid runId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _chatMessageService.SaveObjectAsync(
                new NeuCharWorkflowChatMessage(
                    workflowId,
                    NeuCharWorkflowChatSessionService.HashParticipant(participantKey),
                    role,
                    content,
                    runId)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "保存 NeuChar Workflow Chat 消息失败：WorkflowId={WorkflowId}，Role={Role}。", workflowId, role);
        }
    }

    /// <summary>
    /// Deletes persisted chat messages for one workflow participant without failing the caller
    /// when cleanup storage is unavailable.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="participantKey">The participant key.</param>
    private async Task SafeDeleteChatMessagesByParticipantAsync(
        int workflowId,
        string participantKey,
        CancellationToken cancellationToken)
    {
        try
        {
            await _chatMessageService
                .DeleteByParticipantAsync(workflowId, NeuCharWorkflowChatSessionService.HashParticipant(participantKey), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "清除 NeuChar Workflow Chat 消息失败：WorkflowId={WorkflowId}。", workflowId);
        }
    }

    /// <summary>
    /// Deletes all persisted chat messages for a workflow and suppresses cleanup failures.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    private async Task SafeDeleteChatMessagesByWorkflowAsync(int workflowId)
    {
        try
        {
            await _chatMessageService.DeleteByWorkflowAsync(workflowId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "删除 NeuChar Workflow Chat 消息失败：WorkflowId={WorkflowId}。", workflowId);
        }
    }

    /// <summary>
    /// Validates the bounded participant-key format used by chat sessions.
    /// </summary>
    /// <param name="participantKey">The participant key to validate.</param>
    /// <returns><see langword="true"/> when the key is safe and non-empty.</returns>
    private static bool IsValidParticipantKey(string? participantKey)
    {
        var normalized = participantKey?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 256)
        {
            return false;
        }
        var index = normalized.IndexOf(':');
        return index is > 1 and < 256
            && (normalized[..index] == "user" || normalized[..index] == "guest");
    }

    /// <summary>
    /// Normalizes a workflow output for chat display and applies the chat output size limit.
    /// </summary>
    /// <param name="output">The workflow output text.</param>
    /// <returns>A normalized display string.</returns>
    private static string NormalizeChatOutput(string? output)
    {
        var normalized = (output ?? string.Empty).Trim();
        if (normalized.Length >= 2 && normalized.StartsWith('"') && normalized.EndsWith('"'))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<string>(normalized);
                if (parsed != null)
                {
                    return parsed;
                }
            }
            catch (JsonException)
            {
                //Legacy comment translated to English.
            }
        }
        return normalized.Length == 0 ? "工作流已完成。" : normalized;
    }

    /// <summary>
    /// Validates graph structure, references, nested workflows, and module availability
    /// before an operation that requires an executable workflow.
    /// </summary>
    /// <param name="workflow">The persisted workflow to validate.</param>
    /// <param name="cancellationToken">Token used to cancel validation.</param>
    /// <exception cref="WorkflowInputException">Thrown when the graph or references are invalid.</exception>
    /// <exception cref="WorkflowModuleUnavailableException">Thrown when the module is unavailable.</exception>
    private async Task ValidateWorkflowAsync(WorkflowEntity workflow, CancellationToken cancellationToken)
    {
        NeuCharWorkflowGraph graph;
        try
        {
            graph = _workflowEngine.ParseAndValidateGraph(workflow.GraphJson);
        }
        catch (InvalidOperationException ex)
        {
            throw new WorkflowInputException(ex.Message, ex);
        }
        var validationError = await _workflowEngine.ValidateReferencesAsync(graph, cancellationToken).ConfigureAwait(false);
        if (validationError != null)
        {
            throw new WorkflowInputException(validationError);
        }
        var subWorkflowError = await _workflowEngine.ValidateSubWorkflowReferencesAsync(
            graph,
            workflow.Id,
            workflow.AdminUserId,
            requireEnabled: true,
            cancellationToken).ConfigureAwait(false);
        if (subWorkflowError != null)
        {
            throw new WorkflowInputException(subWorkflowError);
        }
    }

    /// <summary>
    /// Ensures the NeuChar Workflow module is installed and enabled.
    /// </summary>
    /// <exception cref="WorkflowModuleUnavailableException">Thrown when the module is unavailable.</exception>
    private async Task EnsureModuleEnabledAsync()
    {
        if (!await IsModuleEnabledAsync().ConfigureAwait(false))
        {
            throw new WorkflowModuleUnavailableException();
        }
    }

    /// <summary>
    /// Checks whether the NeuChar Workflow module is installed and enabled.
    /// </summary>
    /// <returns>A task whose result indicates module availability.</returns>
    private async Task<bool> IsModuleEnabledAsync()
    {
        var module = await _xncfModuleService.GetObjectAsync(z => z.Uid == new Register().Uid).ConfigureAwait(false);
        return module?.State == XncfModules_State.开放;
    }

    /// <summary>
    /// Persists a workflow version snapshot when the save operation represents a new version.
    /// </summary>
    /// <param name="workflow">The workflow being versioned.</param>
    /// <param name="adminUserId">The owner identifier.</param>
    /// <param name="saveSource">The source label for the version record.</param>
    private async Task SaveVersionAsync(WorkflowEntity workflow, int adminUserId, string? saveSource)
    {
        await _workflowVersionService.SaveObjectAsync(new NeuCharWorkflowVersion(workflow, adminUserId, saveSource))
            .ConfigureAwait(false);
        var versions = await _workflowVersionService.GetFullListAsync(
            z => z.WorkflowId == workflow.Id,
            z => z.Revision,
            OrderingType.Descending).ConfigureAwait(false);
        foreach (var obsolete in versions.Skip(5))
        {
            await _workflowVersionService.DeleteObjectAsync(obsolete).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Loads a workflow only when it belongs to the specified administrator.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="adminUserId">The owner identifier.</param>
    /// <returns>A task containing the owned workflow, or <see langword="null"/> when not found.</returns>
    private Task<WorkflowEntity?> GetOwnedWorkflowAsync(int workflowId, int adminUserId) =>
        _workflowService.GetObjectAsync(z => z.Id == workflowId && z.AdminUserId == adminUserId);

    /// <summary>
    /// Extracts AgentManager workflow references from a graph for save-time validation.
    /// </summary>
    /// <param name="graph">The workflow graph.</param>
    /// <returns>The distinct referenced agent workflow values.</returns>
    private static IReadOnlyList<AgentWorkflowReference> GetAgentWorkflowReferences(
        NeuCharWorkflowGraph graph)
    {
        return (graph?.Nodes ?? new List<NeuCharWorkflowNode>())
            .Where(node => string.Equals(node.Type, "agent", StringComparison.OrdinalIgnoreCase)
                || string.Equals(node.Type, "agent-group", StringComparison.OrdinalIgnoreCase))
            .Select(node => new
            {
                ProviderId = ReadNodeString(node.Config, "providerId"),
                ObjectId = ReadNodeString(node.Config, "objectId")
            })
            .Where(node => string.Equals(node.ProviderId, "agents-manager", StringComparison.OrdinalIgnoreCase))
            .Select(node =>
            {
                if (node.ObjectId.StartsWith("agent:", StringComparison.OrdinalIgnoreCase))
                {
                    return new AgentWorkflowReference(
                        "agent",
                        ParseNodeId(node.ObjectId, "agent:"));
                }

                if (node.ObjectId.StartsWith("group:", StringComparison.OrdinalIgnoreCase))
                {
                    return new AgentWorkflowReference(
                        "group",
                        ParseNodeId(node.ObjectId, "group:"));
                }

                return null;
            })
            .Where(reference => reference?.Id > 0)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Reads a node configuration string and applies a fallback.
    /// </summary>
    /// <param name="node">The node to inspect.</param>
    /// <param name="name">The configuration property name.</param>
    /// <param name="fallback">The fallback value.</param>
    /// <returns>The configured or fallback string.</returns>
    private static string ReadNodeString(
        System.Text.Json.Nodes.JsonObject? config,
        string key)
        => config?.TryGetPropertyValue(key, out var value) == true
            ? value?.ToString() ?? string.Empty
            : string.Empty;

    /// <summary>
    /// Parses an integer node identifier from a prefixed string.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <param name="prefix">The expected prefix.</param>
    /// <returns>The parsed identifier, or zero when parsing fails.</returns>
    private static int ParseNodeId(string value, string prefix)
        => int.TryParse(value[prefix.Length..], out var id) ? id : 0;

    /// <summary>
    /// Loads completed execution logs for task-list cleanup and pagination.
    /// </summary>
    /// <param name="adminUserId">The workflow owner identifier.</param>
    /// <param name="workflowId">Optional workflow filter.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>A task containing matching completed logs.</returns>
    private async Task<List<NeuCharWorkflowExecutionLog>> GetCompletedTaskLogsAsync(
        int adminUserId,
        DateTime cutoff,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workflowIds = (await _workflowService.GetFullListAsync(
            z => z.AdminUserId == adminUserId,
            z => z.Id,
            OrderingType.Ascending).ConfigureAwait(false))
            .Select(z => z.Id)
            .ToList();
        if (workflowIds.Count == 0)
        {
            return new List<NeuCharWorkflowExecutionLog>();
        }

        var logs = await _executionLogService.GetFullListAsync(
            z => workflowIds.Contains(z.WorkflowId) && z.FinishedAt != null && z.FinishedAt <= cutoff,
            z => z.Id,
            OrderingType.Descending).ConfigureAwait(false);
        return logs.ToList();
    }

    /// <summary>
    /// Converts completed execution logs into cleanup-preview items.
    /// </summary>
    /// <param name="logs">The completed logs.</param>
    /// <returns>The cleanup preview.</returns>
    private static WorkflowTaskCleanupPreview ToTaskCleanupPreview(
        IReadOnlyCollection<NeuCharWorkflowExecutionLog> logs,
        DateTime cutoff) => new(
        logs.Count,
        logs.Count(log => log.Succeeded == true),
        logs.Count(log => log.Succeeded != true),
        cutoff);

    /// <summary>
    /// Validates required workflow save fields and graph size limits.
    /// </summary>
    /// <param name="request">The save command to validate.</param>
    /// <exception cref="WorkflowInputException">Thrown when save input is invalid.</exception>
    private static void ValidateSaveCommand(SaveWorkflowCommand? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new WorkflowInputException("工作流名称不能为空。");
        }
        if (request.Name.Trim().Length > 200)
        {
            throw new WorkflowInputException("工作流名称不能超过 200 个字符。");
        }
        if (request.Description?.Length > 10_000 || request.TriggerConfigJson?.Length > 100_000)
        {
            throw new WorkflowInputException("工作流描述或触发器配置超过允许长度。");
        }
        if (request.AutoSaveMinutes is < 0 or > 1440)
        {
            throw new WorkflowInputException("自动保存间隔必须为 0 到 1440 分钟，0 表示关闭。");
        }
    }

    /// <summary>
    /// Validates the workflow identifier and initial run input size.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="input">The initial input text.</param>
    /// <exception cref="WorkflowInputException">Thrown when the identifier or input is invalid.</exception>
    private static void ValidateRunInput(int workflowId, string? input)
    {
        if (workflowId <= 0 || input?.Length > 100_000)
        {
            throw new WorkflowInputException("工作流测试请求无效，输入不能超过 100000 个字符。");
        }
    }

    /// <summary>
    /// Determines whether a save request leaves all persisted workflow fields unchanged.
    /// </summary>
    /// <param name="workflow">The persisted workflow.</param>
    /// <param name="request">The requested changes.</param>
    /// <param name="graphJson">The normalized graph JSON.</param>
    /// <param name="enabled">The normalized enabled state.</param>
    /// <param name="triggerType">The normalized trigger type.</param>
    /// <param name="triggerConfigJson">The normalized trigger configuration.</param>
    /// <returns><see langword="true"/> when no persisted value changes.</returns>
    private static bool IsUnchanged(WorkflowEntity workflow, SaveWorkflowCommand request, string graphJson, bool enabled,
        string triggerType, string triggerConfigJson, int autoSaveMinutes) =>
        string.Equals(workflow.Name, request.Name?.Trim(), StringComparison.Ordinal) &&
        string.Equals(workflow.Description, request.Description?.Trim(), StringComparison.Ordinal) &&
        string.Equals(workflow.GraphJson, graphJson, StringComparison.Ordinal) &&
        workflow.Enabled == enabled &&
        string.Equals(workflow.TriggerType, triggerType, StringComparison.Ordinal) &&
        string.Equals(workflow.TriggerConfigJson, triggerConfigJson, StringComparison.Ordinal) &&
        workflow.AutoSaveMinutes == autoSaveMinutes;

    /// <summary>
    /// Reads recent replay events and collects the first observed output schema for each node.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="cancellationToken">Token used to cancel log reads.</param>
    /// <returns>A task containing observed schemas.</returns>
    private async Task<IReadOnlyList<NeuCharWorkflowObservedOutputSchema>> ReadObservedOutputSchemasAsync(
        int workflowId,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, NeuCharWorkflowObservedOutputSchema>(StringComparer.Ordinal);
        var replayEvents = await _executionLogService.GetRecentCompletedReplayEventsAsync(
                workflowId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        foreach (var replayEventsJson in replayEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddObservedSchemas(result, replayEventsJson);
        }
        return result.Values.ToList();
    }

    /// <summary>
    /// Adds successful-run output schemas from one replay-event JSON payload,
    /// ignoring malformed historical logs.
    /// </summary>
    /// <param name="result">The schema map receiving new entries.</param>
    /// <param name="replayEventsJson">Serialized replay events.</param>
    private static void AddObservedSchemas(
        IDictionary<string, NeuCharWorkflowObservedOutputSchema> result,
        string? replayEventsJson)
    {
        if (string.IsNullOrWhiteSpace(replayEventsJson))
        {
            return;
        }
        try
        {
            var events = JsonSerializer.Deserialize<List<NeuCharWorkflowProgress>>(
                replayEventsJson,
                DesignerJsonOptions) ?? new List<NeuCharWorkflowProgress>();
            foreach (var item in events)
            {
                if (!string.Equals(item.Status, "success", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (string.IsNullOrWhiteSpace(item.OutputSchema))
                {
                    continue;
                }
                var schema = JsonSerializer.Deserialize<NeuCharWorkflowObservedOutputSchema>(
                    item.OutputSchema,
                    DesignerJsonOptions);
                if (schema != null && !result.ContainsKey(schema.NodeId))
                {
                    result[schema.NodeId] = schema;
                }
            }
        }
        catch (JsonException)
        {
            // A historical log must not prevent the workflow editor from opening.
        }
    }

    /// <summary>
    /// Maps a persisted workflow entity to a workflow-detail DTO.
    /// </summary>
    /// <param name="workflow">The workflow entity.</param>
    /// <param name="graphJson">Optional editable graph JSON.</param>
    /// <param name="unchanged">Whether the latest save was unchanged.</param>
    /// <param name="runningCount">The current running count.</param>
    /// <param name="observedOutputSchemas">Observed output schemas.</param>
    /// <returns>The workflow detail DTO.</returns>
    private static WorkflowDetail ToDetail(
        WorkflowEntity workflow,
        string? graphJson = null,
        bool unchanged = false,
        int runningCount = 0,
        IReadOnlyList<NeuCharWorkflowObservedOutputSchema>? observedOutputSchemas = null) => new(
        workflow.Id, workflow.Name, workflow.Description, graphJson ?? workflow.GraphJson, workflow.Enabled,
        workflow.TriggerType, workflow.TriggerConfigJson, workflow.NextRunAt, workflow.LastRunAt, workflow.LastSucceeded,
        workflow.LastError, workflow.Revision, workflow.AutoSaveMinutes, unchanged, workflow.LastUpdateTime,
        runningCount, observedOutputSchemas);

    /// <summary>
    /// Maps a persisted workflow entity to a list-item DTO.
    /// </summary>
    /// <param name="workflow">The workflow entity.</param>
    /// <returns>The list-item DTO.</returns>
    private static WorkflowListItem ToListItem(WorkflowEntity workflow) => new(
        workflow.Id, workflow.Name, workflow.Description, workflow.Enabled, workflow.TriggerType, workflow.NextRunAt,
        workflow.LastRunAt, workflow.LastSucceeded, workflow.LastError, workflow.Revision, workflow.AutoSaveMinutes,
        workflow.LastUpdateTime);

    /// <summary>
    /// Maps an execution log to the task-list status string.
    /// </summary>
    /// <param name="log">The execution log.</param>
    /// <returns><c>running</c>, <c>success</c>, or <c>failed</c>.</returns>
    private static string ToTaskStatus(NeuCharWorkflowExecutionLog log) => log.FinishedAt == null
        ? "running"
        : log.Succeeded == true ? "success" : "failed";

    /// <summary>
    /// Determines whether an execution log has enough persisted data for replay.
    /// </summary>
    /// <param name="log">The execution log.</param>
    /// <returns><see langword="true"/> when snapshot and replay events are available.</returns>
    private static bool CanReplay(NeuCharWorkflowExecutionLog log) =>
        log.FinishedAt != null &&
        !string.IsNullOrWhiteSpace(log.ReplaySnapshotHash) &&
        !string.IsNullOrWhiteSpace(log.ReplayEventsJson);

    /// <summary>
    /// Builds a short human-readable summary from an active run's latest node state.
    /// </summary>
    /// <param name="run">The active run.</param>
    /// <returns>The live summary text.</returns>
    private static string? BuildLiveSummary(NeuCharWorkflowActiveRun run)
    {
        if (string.IsNullOrWhiteSpace(run.LastNodeName) && string.IsNullOrWhiteSpace(run.LastMessage))
        {
            return "等待工作流引擎开始执行。";
        }

        var node = string.IsNullOrWhiteSpace(run.LastNodeName) ? "工作流" : run.LastNodeName;
        var message = string.IsNullOrWhiteSpace(run.LastMessage) ? "正在执行" : run.LastMessage;
        return $"{node}：{message}";
    }

    /// <summary>Converts a DateTime value to a UTC DateTimeOffset.</summary>
    /// <param name="value">The DateTime value.</param>
    /// <returns>The UTC offset value.</returns>
    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    /// <summary>Converts an optional DateTime value to a UTC DateTimeOffset.</summary>
    /// <param name="value">The optional DateTime value.</param>
    /// <returns>The converted value, or <see langword="null"/>.</returns>
    private static DateTimeOffset? ToUtcOffset(DateTime? value) =>
        value.HasValue ? ToUtcOffset(value.Value) : null;

    /// <summary>
    /// Combines in-process and persisted unfinished-run counts, returning the larger value.
    /// </summary>
    /// <param name="adminUserId">The workflow owner identifier.</param>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the persisted-count query.</param>
    /// <returns>A task containing the running count.</returns>
    private async Task<int> GetRunningCountAsync(
        int adminUserId,
        int workflowId,
        CancellationToken cancellationToken)
    {
        var activeCount = _runCoordinator.GetActiveRunCount(adminUserId, workflowId);
        var persistedCount = await _executionLogService
            .GetUnfinishedCountAsync(workflowId, cancellationToken)
            .ConfigureAwait(false);
        return Math.Max(activeCount, persistedCount);
    }

    /// <summary>
    /// Extracts a run GUID from the persisted workflow correlation-ID format.
    /// </summary>
    /// <param name="workflowId">The workflow identifier expected in the prefix.</param>
    /// <param name="correlationId">The correlation ID to parse.</param>
    /// <returns>The run ID, or <see langword="null"/> when the format does not match.</returns>
    private static Guid? GetTaskRunId(int workflowId, string? correlationId)
    {
        var prefix = $"workflow-{workflowId}-run-";
        if (string.IsNullOrWhiteSpace(correlationId) ||
            !correlationId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        return Guid.TryParse(correlationId[prefix.Length..], out var runId) ? runId : null;
    }

    /// <summary>
    /// Creates a bounded name for a replay-created workflow draft.
    /// </summary>
    /// <param name="sourceName">The source workflow name.</param>
    /// <returns>The copy name with the task-copy suffix.</returns>
    private static string CreateReplayCopyName(string? sourceName)
    {
        const string suffix = "（任务副本）";
        var prefix = string.IsNullOrWhiteSpace(sourceName) ? "Workflow" : sourceName.Trim();
        return prefix.Length + suffix.Length <= 200
            ? prefix + suffix
            : prefix[..(200 - suffix.Length)] + suffix;
    }

    /// <summary>
    /// Compares two non-empty tokens using a fixed-time byte comparison.
    /// </summary>
    /// <param name="expected">The expected token.</param>
    /// <param name="actual">The supplied token.</param>
    /// <returns><see langword="true"/> when both tokens match.</returns>
    private static bool TokensEqual(string? expected, string? actual)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual))
        {
            return false;
        }
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return expectedBytes.Length == actualBytes.Length && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    /// <summary>
    /// Determines whether a supported input value is null, whitespace, or an empty collection.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns><see langword="true"/> when the value is empty.</returns>
    private static bool IsEmpty(object? value) => value switch
    {
        null => true,
        string text => string.IsNullOrWhiteSpace(text),
        string[] values => values.Length == 0 || values.All(string.IsNullOrWhiteSpace),
        JsonElement element => element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ||
            element.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(element.GetString()),
        _ => false
    };
}

public sealed record SaveWorkflowCommand(
    int Id,
    string? Name,
    string? Description,
    string? GraphJson,
    bool Enabled,
    string? TriggerType,
    string? TriggerConfigJson,
    int AutoSaveMinutes = 3,
    int? ExpectedRevision = null,
    string? SaveSource = null);

public sealed record WorkflowListItem(
    int Id, string Name, string? Description, bool Enabled, string TriggerType, DateTime? NextRunAt,
    DateTime? LastRunAt, bool? LastSucceeded, string? LastError, int Revision, int AutoSaveMinutes, DateTime LastUpdateTime);

public sealed record WorkflowDetail(
    int Id, string Name, string? Description, string GraphJson, bool Enabled, string TriggerType, string TriggerConfigJson,
    DateTime? NextRunAt, DateTime? LastRunAt, bool? LastSucceeded, string? LastError, int Revision, int AutoSaveMinutes,
    bool Unchanged, DateTime LastUpdateTime,
    int RunningCount = 0,
    IReadOnlyList<NeuCharWorkflowObservedOutputSchema>? ObservedOutputSchemas = null);

public sealed record WorkflowInterchangeExport(
    string FileName,
    string ContentType,
    string Content);

public sealed record WorkflowTaskListItem(
    string TaskId,
    int WorkflowId,
    string WorkflowName,
    string Source,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    string? Summary,
    string? ErrorMessage,
    Guid? RunId,
    int? ExecutionLogId,
    bool ReplayAvailable);

/// <summary>
/// Represents one page of workflow execution tasks and optional analytics summary.
/// </summary>
/// <param name="Items">The task items in descending start order.</param>
/// <param name="HasMore">Whether older persisted tasks remain.</param>
/// <param name="NextExecutionLogId">The cursor for the next page.</param>
/// <param name="Summary">Optional aggregate analytics for the query.</param>
public sealed record WorkflowTaskListPage(
    IReadOnlyList<WorkflowTaskListItem> Items,
    bool HasMore,
    int? NextExecutionLogId,
    WorkflowAnalyticsSummary? Summary = null);

/// <summary>
/// Summarizes completed workflow tasks eligible for cleanup at a cutoff.
/// </summary>
/// <param name="CompletedCount">The number of completed tasks.</param>
/// <param name="SucceededCount">The number of successful tasks.</param>
/// <param name="FailedCount">The number of failed tasks.</param>
/// <param name="Cutoff">The cleanup cutoff timestamp.</param>
public sealed record WorkflowTaskCleanupPreview(
    int CompletedCount,
    int SucceededCount,
    int FailedCount,
    DateTime Cutoff);

/// <summary>
/// Reports the number of execution logs deleted by a cleanup operation.
/// </summary>
/// <param name="DeletedCount">The number of deleted logs.</param>
/// <param name="Cutoff">The effective UTC cutoff used for deletion.</param>
public sealed record WorkflowTaskCleanupResult(
    int DeletedCount,
    DateTime Cutoff);

/// <summary>
/// Represents one persisted event in a workflow replay sequence.
/// </summary>
public sealed record WorkflowReplayEvent(
    int Sequence,
    string NodeId,
    string NodeName,
    string Status,
    string Message,
    string? Output,
    DateTimeOffset Timestamp,
    string? OutputSchema = null,
    string? Input = null)
{
    /// <summary>
    /// Gets or initializes the optional object reference emitted by the replayed node.
    /// </summary>
    public WorkflowObjectExecutionReference? ObjectReference { get; init; }
}

/// <summary>
/// Contains the persisted workflow definition and ordered replay events for one execution.
/// </summary>
public sealed record WorkflowRunReplay(
    int ExecutionLogId,
    int WorkflowId,
    string WorkflowName,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    bool? Succeeded,
    string? ResultSummary,
    string? ErrorMessage,
    NeuCharWorkflowReplayDefinition Definition,
    IReadOnlyList<WorkflowReplayEvent> Events);

/// <summary>
/// Describes one Function available in the Workflow designer.
/// </summary>
public sealed record WorkflowDesignerFunction(
    string FunctionKey, string Name, string? Description, string ModuleUid, string ModuleName, string ModuleVersion,
    bool ModuleAvailable, string ModuleState, string ParameterSchemaJson, string DefaultParametersJson,
    NeuCharFunctionOutputDescriptor? Output, string? CatalogError)
{
    /// <summary>
    /// Gets the compatibility identifier retained for legacy designer consumers.
    /// </summary>
    public int Id => 0;
    /// <summary>
    /// Gets the display name through the legacy property name.
    /// </summary>
    public string FunctionName => Name;
}

/// <summary>
/// Contains the Function catalog and executable object catalog used by the designer.
/// </summary>
public sealed record WorkflowDesignerData(
    IReadOnlyList<WorkflowDesignerFunction> Functions,
    IReadOnlyList<Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow.WorkflowObjectDescriptor> Objects);

public sealed record WorkflowWebhookTriggerResult(
    int StatusCode,
    string? ErrorMessage = null,
    string? AllowedMethod = null,
    IReadOnlyList<string>? MissingParameters = null,
    int? WorkflowId = null,
    Guid? RunId = null)
{
    /// <summary>Creates a 404 webhook result for a missing workflow.</summary>
    public static WorkflowWebhookTriggerResult NotFound() => new(404, "工作流不存在。");
    /// <summary>Creates a 409 webhook result for a workflow conflict.</summary>
    public static WorkflowWebhookTriggerResult Conflict(string? error) => new(409, error);
    /// <summary>Creates a 500 webhook result for an internal failure.</summary>
    public static WorkflowWebhookTriggerResult ServerError(string? error) => new(500, error);
    /// <summary>Creates a 405 result and reports the allowed HTTP method.</summary>
    public static WorkflowWebhookTriggerResult MethodNotAllowed(string method) => new(405, $"Webhook 只接受 {method.ToUpperInvariant()} 请求。", method);
    /// <summary>Creates a 401 result for an invalid webhook token.</summary>
    public static WorkflowWebhookTriggerResult Unauthorized() => new(401, "Webhook 访问密钥无效。");
    /// <summary>Creates a 400 result for missing webhook parameters.</summary>
    public static WorkflowWebhookTriggerResult BadRequest(string error, IReadOnlyList<string> missing) => new(400, error, MissingParameters: missing);
    /// <summary>Creates a 202 result for an accepted webhook-triggered run.</summary>
    public static WorkflowWebhookTriggerResult Accepted(int workflowId, Guid runId) => new(202, WorkflowId: workflowId, RunId: runId);
}

/// <summary>
/// Represents one chat message returned by the Workflow chat surface.
/// </summary>
/// <param name="Role">The message role.</param>
/// <param name="Content">The message content.</param>
/// <param name="Timestamp">The UTC message timestamp.</param>
public sealed record WorkflowChatMessage(
    string Role,
    string Content,
    DateTimeOffset Timestamp);

/// <summary>
/// Describes whether a participant may access a chat-triggered workflow.
/// </summary>
/// <param name="Ok">Whether access is permitted.</param>
/// <param name="StatusCode">The HTTP-style status code for the decision.</param>
/// <param name="ErrorMessage">The failure message when access is denied.</param>
/// <param name="Workflow">The accessible workflow entity.</param>
/// <param name="Config">The parsed chat configuration.</param>
/// <param name="IsGuest">Whether the participant is an anonymous guest.</param>
public sealed record WorkflowChatAccess(
    bool Ok,
    int StatusCode,
    string? ErrorMessage = null,
    WorkflowEntity? Workflow = null,
    NeuCharWorkflowChatConfig? Config = null,
    bool IsGuest = false)
{
    /// <summary>Creates a successful workflow-chat access result.</summary>
    public static WorkflowChatAccess Success(WorkflowEntity workflow, NeuCharWorkflowChatConfig config, bool isGuest)
        => new(true, 200, Workflow: workflow, Config: config, IsGuest: isGuest);
    /// <summary>Creates a not-found chat-access result.</summary>
    public static WorkflowChatAccess NotFound() => new(false, 404, "工作流不存在。");
    /// <summary>Creates a conflict chat-access result.</summary>
    public static WorkflowChatAccess Conflict(string? error) => new(false, 409, error);
    /// <summary>Creates an unauthorized chat-access result.</summary>
    public static WorkflowChatAccess Unauthorized() => new(false, 401, "该工作流未开启访客访问，请先登录。");
    /// <summary>Creates a server-error chat-access result.</summary>
    public static WorkflowChatAccess ServerError(string? error) => new(false, 500, error);
}

/// <summary>
/// Represents the bootstrap response returned before a chat session begins.
/// </summary>
public sealed record WorkflowChatBootstrapResult(
    int StatusCode,
    string? ErrorMessage = null,
    int? WorkflowId = null,
    string? Title = null,
    string? Greeting = null,
    bool IsGuest = false,
    bool HasPendingRun = false,
    Guid? PendingRunId = null,
    IReadOnlyList<WorkflowChatMessage>? Messages = null)
{
    /// <summary>Creates a not-found chat-bootstrap result.</summary>
    public static WorkflowChatBootstrapResult NotFound() => new(404, "工作流不存在。");
    /// <summary>Creates a bad-request chat-bootstrap result.</summary>
    public static WorkflowChatBootstrapResult BadRequest(string error) => new(400, error);
    /// <summary>Maps an access result into a bootstrap response.</summary>
    public static WorkflowChatBootstrapResult From(WorkflowChatAccess access) => new(access.StatusCode, access.ErrorMessage);
    /// <summary>Creates a successful chat-bootstrap result with session metadata.</summary>
    public static WorkflowChatBootstrapResult Ok(
        int workflowId,
        string title,
        string greeting,
        bool isGuest,
        bool hasPendingRun,
        Guid? pendingRunId,
        IReadOnlyList<WorkflowChatMessage> messages)
        => new(200, WorkflowId: workflowId, Title: title, Greeting: greeting, IsGuest: isGuest, HasPendingRun: hasPendingRun, PendingRunId: pendingRunId, Messages: messages);
}

/// <summary>
/// Represents the response returned after a chat message starts a workflow run.
/// </summary>
public sealed record WorkflowChatSendResult(
    int StatusCode,
    string? ErrorMessage = null,
    int? WorkflowId = null,
    Guid? RunId = null)
{
    /// <summary>Creates a not-found chat-send result.</summary>
    public static WorkflowChatSendResult NotFound() => new(404, "工作流不存在。");
    /// <summary>Creates a bad-request chat-send result.</summary>
    public static WorkflowChatSendResult BadRequest(string error) => new(400, error);
    /// <summary>Creates a conflict chat-send result.</summary>
    public static WorkflowChatSendResult Conflict(string? error) => new(409, error);
    /// <summary>Maps an access result into a send response.</summary>
    public static WorkflowChatSendResult From(WorkflowChatAccess access) => new(access.StatusCode, access.ErrorMessage);
    /// <summary>Creates an accepted chat-send result with the started run ID.</summary>
    public static WorkflowChatSendResult Accepted(int workflowId, Guid runId) => new(202, WorkflowId: workflowId, RunId: runId);
}

/// <summary>
/// Represents the current or completed status of a chat-triggered workflow run.
/// </summary>
public sealed record WorkflowChatRunResult(
    int StatusCode,
    string? ErrorMessage = null,
    int? WorkflowId = null,
    Guid? RunId = null,
    bool Running = false,
    string? FinalOutput = null,
    string? RunError = null,
    string? LastNodeMessage = null,
    long LastSequence = 0)
{
    /// <summary>Creates a not-found chat-run result.</summary>
    public static WorkflowChatRunResult NotFound() => new(404, "运行任务不存在或不属于当前会话。");
    /// <summary>Creates a bad-request chat-run result.</summary>
    public static WorkflowChatRunResult BadRequest(string error) => new(400, error);
    /// <summary>Maps an access result into a run-status response.</summary>
    public static WorkflowChatRunResult From(WorkflowChatAccess access) => new(access.StatusCode, access.ErrorMessage);
    /// <summary>Creates an in-progress run-status result.</summary>
    public static WorkflowChatRunResult InProgress(int workflowId, Guid runId, string lastNodeMessage, long lastSequence)
        => new(200, WorkflowId: workflowId, RunId: runId, Running: true, LastNodeMessage: lastNodeMessage, LastSequence: lastSequence);
    /// <summary>Creates a successful completed run-status result.</summary>
    public static WorkflowChatRunResult Succeeded(int workflowId, Guid runId, string finalOutput)
        => new(200, WorkflowId: workflowId, RunId: runId, FinalOutput: finalOutput);
    /// <summary>Creates a failed completed run-status result.</summary>
    public static WorkflowChatRunResult Failed(int workflowId, Guid runId, string? error)
        => new(200, WorkflowId: workflowId, RunId: runId, RunError: error);
}

/// <summary>
/// Represents the result of a chat-session reset operation.
/// </summary>
public sealed record WorkflowChatResult(
    int StatusCode,
    string? ErrorMessage = null)
{
    /// <summary>Creates a not-found chat result.</summary>
    public static WorkflowChatResult NotFound() => new(404, "工作流不存在。");
    /// <summary>Creates a bad-request chat result.</summary>
    public static WorkflowChatResult BadRequest(string error) => new(400, error);
    /// <summary>Maps an access result into a chat response.</summary>
    public static WorkflowChatResult From(WorkflowChatAccess access) => new(access.StatusCode, access.ErrorMessage);
    /// <summary>Creates an empty successful chat result.</summary>
    public static WorkflowChatResult Ok() => new(200);
}

/// <summary>
/// Indicates that a Workflow request or graph cannot be accepted because its input is invalid.
/// </summary>
public sealed class WorkflowInputException : InvalidOperationException
{
    /// <summary>
    /// Initializes an input exception with a diagnostic message and optional cause.
    /// </summary>
    /// <param name="message">The validation message.</param>
    /// <param name="innerException">The underlying parsing or validation exception.</param>
    public WorkflowInputException(string message, Exception? innerException = null) : base(message, innerException) { }
}

public sealed class WorkflowNotFoundException : InvalidOperationException
{
    public WorkflowNotFoundException() : base("工作流不存在或没有访问权限。") { }
}

public sealed class WorkflowConflictException : InvalidOperationException
{
    public WorkflowConflictException(string? message) : base(message) { }
}

public sealed class WorkflowModuleUnavailableException : InvalidOperationException
{
    public WorkflowModuleUnavailableException() : base("NeuChar Workflow 模块未安装或未开启。") { }
}
