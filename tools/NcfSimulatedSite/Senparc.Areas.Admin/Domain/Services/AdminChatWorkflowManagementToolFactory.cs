/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatWorkflowManagementToolFactory.cs
    File description: Approval-gated AI Workflow creation tools

----------------------------------------------------------------*/

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Localization;
using Senparc.Areas.Admin;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
/// Exposes Workflow design metadata and creation only to the native AdminChat
/// Harness. Saving is wrapped in ApprovalRequiredAIFunction so the model cannot
/// create or enable a Workflow without an explicit administrator decision.
/// </summary>
public static class AdminChatWorkflowManagementToolFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<AIFunction> Create(
        IWorkflowCreationProvider provider,
        int adminUserId,
        IStringLocalizer<AdminResource>? localizer = null)
    {
        ArgumentNullException.ThrowIfNull(provider);
        if (adminUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(adminUserId));
        }

        var tools = new List<AIFunction>
        {
            new CapabilitiesTool(provider, adminUserId, localizer),
            new DefinitionTool(provider, adminUserId, localizer, validateOnly: true)
        };

        var createTool = new DefinitionTool(provider, adminUserId, localizer, validateOnly: false);
        tools.Add(new ApprovalRequiredAIFunction(createTool));
        return tools;
    }

    private abstract class WorkflowToolBase : AIFunction
    {
        protected readonly IWorkflowCreationProvider Provider;
        protected readonly int AdminUserId;
        protected readonly IStringLocalizer<AdminResource>? Localizer;

        protected WorkflowToolBase(
            IWorkflowCreationProvider provider,
            int adminUserId,
            IStringLocalizer<AdminResource>? localizer)
        {
            Provider = provider;
            AdminUserId = adminUserId;
            Localizer = localizer;
        }

        protected string Text(string key, string fallback) =>
            Localizer?[key].ResourceNotFound == false
                ? Localizer[key].Value
                : fallback;

        protected static string StringArgument(AIFunctionArguments arguments, string name) =>
            arguments != null && arguments.TryGetValue(name, out var value)
                ? value switch
                {
                    null => string.Empty,
                    string text => text,
                    JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString() ?? string.Empty,
                    JsonElement element => element.GetRawText(),
                    _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
                }
                : string.Empty;

        protected static bool BoolArgument(AIFunctionArguments arguments, string name, bool fallback) =>
            arguments != null && arguments.TryGetValue(name, out var value)
                ? value switch
                {
                    bool boolean => boolean,
                    JsonElement element when element.ValueKind is JsonValueKind.True or JsonValueKind.False =>
                        element.GetBoolean(),
                    _ when bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) => parsed,
                    _ => fallback
                }
                : fallback;

        protected static JsonElement Schema(object value)
        {
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(value, SerializerOptions));
            return document.RootElement.Clone();
        }
    }

    private sealed class CapabilitiesTool : WorkflowToolBase
    {
        public CapabilitiesTool(
            IWorkflowCreationProvider provider,
            int adminUserId,
            IStringLocalizer<AdminResource>? localizer)
            : base(provider, adminUserId, localizer)
        {
        }

        public override string Name => "workflow_get_creation_capabilities";

        public override string Description =>
            Text(
                "AdminChat.WorkflowCreation.Capabilities.Description",
                "List the safe trigger, node, and FunctionRender choices available for AI Workflow creation. Call this before designing a graph.");

        public override JsonElement JsonSchema => Schema(new
        {
            type = "object",
            properties = new { },
            additionalProperties = false
        });

        protected override async ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            var capabilities = await Provider
                .GetCapabilitiesAsync(AdminUserId, cancellationToken)
                .ConfigureAwait(false);
            return JsonSerializer.Serialize(capabilities, SerializerOptions);
        }
    }

    private sealed class DefinitionTool : WorkflowToolBase
    {
        private readonly bool _validateOnly;

        public DefinitionTool(
            IWorkflowCreationProvider provider,
            int adminUserId,
            IStringLocalizer<AdminResource>? localizer,
            bool validateOnly)
            : base(provider, adminUserId, localizer)
        {
            _validateOnly = validateOnly;
        }

        public override string Name =>
            _validateOnly ? "workflow_validate_creation" : "workflow_create";

        public override string Description =>
            _validateOnly
                ? Text(
                    "AdminChat.WorkflowCreation.Validate.Description",
                    "Validate a proposed safe Workflow graph without saving it. The graph must contain one manual trigger and one end node.")
                : Text(
                    "AdminChat.WorkflowCreation.Create.Description",
                    "Create and optionally enable a validated Workflow. This operation always requires explicit administrator approval.");

        public override JsonElement JsonSchema => Schema(new
        {
            type = "object",
            properties = new
            {
                name = new
                {
                    type = "string",
                    description = Text(
                        "AdminChat.WorkflowCreation.Name",
                        "Workflow name, at most 200 characters.")
                },
                description = new
                {
                    type = "string",
                    description = Text(
                        "AdminChat.WorkflowCreation.DescriptionField",
                        "Optional Workflow description.")
                },
                triggerType = new
                {
                    type = "string",
                    @enum = new[] { "manual" },
                    description = Text(
                        "AdminChat.WorkflowCreation.Trigger",
                        "The only supported AI-created trigger is manual.")
                },
                graphJson = new
                {
                    type = "string",
                    description = Text(
                        "AdminChat.WorkflowCreation.Graph",
                        "JSON graph with nodes and edges. Use node types and FunctionRender references returned by workflow_get_creation_capabilities.")
                },
                enabled = new
                {
                    type = "boolean",
                    description = Text(
                        "AdminChat.WorkflowCreation.Enabled",
                        "Enable after creation. Manual-trigger Workflows remain operator-triggered.")
                }
            },
            required = new[] { "name", "graphJson" },
            additionalProperties = false
        });

        public override JsonElement? ReturnJsonSchema =>
            Schema(new { type = "object" });

        protected override async ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            var request = new WorkflowCreationRequest(
                StringArgument(arguments, "name"),
                StringArgument(arguments, "description"),
                string.IsNullOrWhiteSpace(StringArgument(arguments, "triggerType"))
                    ? "manual"
                    : StringArgument(arguments, "triggerType"),
                StringArgument(arguments, "graphJson"),
                BoolArgument(arguments, "enabled", true));

            if (_validateOnly)
            {
                var validation = await Provider
                    .ValidateAsync(AdminUserId, request, cancellationToken)
                    .ConfigureAwait(false);
                return JsonSerializer.Serialize(validation, SerializerOptions);
            }

            var result = await Provider
                .CreateAsync(AdminUserId, request, cancellationToken)
                .ConfigureAwait(false);
            return JsonSerializer.Serialize(result, SerializerOptions);
        }
    }
}
