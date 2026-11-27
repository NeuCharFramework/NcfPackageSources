/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentTemplateWorkflowAIFunction.cs
    File description: AgentTemplateWorkflowAIFunction.cs related implementation

    Created by: Senparc - 20260822

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/

using Microsoft.Extensions.AI;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.Xncf.AgentsManager;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

internal sealed class AgentTemplateWorkflowAIFunction : AIFunction
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IWorkflowFunctionCallingProvider _provider;
    private readonly int _adminUserId;
    private readonly WorkflowFunctionCallingDescriptor _workflow;
    private readonly JsonElement _jsonSchema;

    /// <summary>
    /// Initializes an AI function wrapper for one owner-scoped workflow.
    /// </summary>
    /// <param name="provider">The provider used to execute the workflow.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="workflow">The callable workflow descriptor.</param>
    /// <exception cref="ArgumentNullException">Thrown when the provider or workflow descriptor is null.</exception>
    public AgentTemplateWorkflowAIFunction(
        IWorkflowFunctionCallingProvider provider,
        int adminUserId,
        WorkflowFunctionCallingDescriptor workflow)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _adminUserId = adminUserId;
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _jsonSchema = BuildJsonSchema(workflow);
    }

    public override string Name => $"Workflow_{_workflow.Id}";

    public override string Description =>
        string.IsNullOrWhiteSpace(_workflow.Description)
            ? AgentsManagerResource.Format(
                "Function.Workflow.Execute.Description",
                "Execute workflow \"{0}\".",
                _workflow.Name)
            : AgentsManagerResource.Format(
                "Function.Workflow.Execute.WithDescription",
                "Execute workflow \"{0}\": {1}",
                _workflow.Name,
                _workflow.Description);

    public override JsonElement JsonSchema => _jsonSchema;

    public override JsonElement? ReturnJsonSchema =>
        JsonDocument.Parse("{\"type\":\"string\"}").RootElement.Clone();

    /// <inheritdoc/>
    /// <remarks>
    /// Converts the required input and optional named arguments into a workflow invocation;
    /// empty input is rejected before execution.
    /// </remarks>
    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        var input = ConvertToString(arguments.TryGetValue("input", out var inputValue)
            ? inputValue
            : null);
        if (string.IsNullOrWhiteSpace(input))
        {
            return AgentsManagerResource.Get(
                "Error.Workflow.InputRequired",
                "Workflow invocation failed: the input parameter cannot be empty.");
        }

        var parameters = arguments
            .Where(pair => !string.Equals(pair.Key, "input", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var result = await _provider.ExecuteAsync(
            _workflow.Id,
            _adminUserId,
            input,
            parameters,
            cancellationToken).ConfigureAwait(false);

        return result.Success
            ? result.Output ?? string.Empty
            : AgentsManagerResource.Format(
                "Error.Workflow.InvocationFailed",
                "Workflow invocation failed: {0}",
                result.ErrorMessage ?? AgentsManagerResource.Get(
                    "Error.Workflow.Unknown",
                    "Unknown error"));
    }

    /// <summary>
    /// Builds the JSON Schema exposed to the model for workflow invocation arguments.
    /// The primary input is required and descriptor parameters are optional string properties.
    /// </summary>
    /// <param name="workflow">The callable workflow descriptor.</param>
    /// <returns>A cloned JSON Schema element.</returns>
    private static JsonElement BuildJsonSchema(WorkflowFunctionCallingDescriptor workflow)
    {
        var properties = new Dictionary<string, object?>
        {
            ["input"] = new
            {
                type = "string",
                description = AgentsManagerResource.Get(
                    "Function.Workflow.Input.Description",
                    "General input passed to the workflow.")
            }
        };

        foreach (var parameter in workflow.Parameters ?? Array.Empty<WorkflowFunctionCallingParameter>())
        {
            if (string.IsNullOrWhiteSpace(parameter.Name)
                || string.Equals(parameter.Name, "input", StringComparison.OrdinalIgnoreCase)
                || properties.ContainsKey(parameter.Name))
            {
                continue;
            }

            properties[parameter.Name] = new
            {
                type = "string",
                description = string.IsNullOrWhiteSpace(parameter.Description)
                    ? AgentsManagerResource.Format(
                        "Function.Workflow.Parameter.Optional",
                        "Workflow parameter \"{0}\" (optional).",
                        parameter.Name)
                    : $"{parameter.Description}{AgentsManagerResource.Get(
                        "Function.Workflow.Parameter.OptionalSuffix",
                        " (optional)")}"
            };
        }

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(
            new
            {
                type = "object",
                properties,
                required = new[] { "input" },
                additionalProperties = false
            },
            SerializerOptions));
        return document.RootElement.Clone();
    }

    /// <summary>
    /// Converts supported AI-function argument values to invariant text.
    /// JSON elements retain their raw JSON representation except for string values.
    /// </summary>
    /// <param name="value">The argument value to convert.</param>
    /// <returns>The invariant string representation.</returns>
    private static string ConvertToString(object value)
        => value switch
        {
            null => string.Empty,
            string text => text,
            JsonElement element when element.ValueKind == JsonValueKind.String =>
                element.GetString() ?? string.Empty,
            JsonElement element => element.GetRawText(),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
}
