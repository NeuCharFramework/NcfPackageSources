/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatFunctionToolFactory.cs
    File description: AdminChatFunctionToolFactory.cs related implementation

    Created by: Senparc - 20260820

    Modified by: Senparc - 20260822
    Change description: v0.6.0 Updated module behavior and related support.

    Modified by: Senparc - 20260917
    Change description: v0.10.0 Updated module behavior and related support.

    Modified by: Senparc - 20261018
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261021
    Change description: v0.11.1 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.12.15 enhance Sandbox functionality with language validation and request handling improvements

    Modified by: Senparc - 20261111
    Change description: v0.12.28 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Microsoft.Extensions.AI;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
/// Creates invocable AdminChat tools from FunctionRender methods.
/// </summary>
public static class AdminChatFunctionToolFactory
{
    public static AIFunction Create(
        MethodInfo method,
        object target,
        string name,
        string description,
        IReadOnlyList<FunctionParameterInfo> parameterInfos = null)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!method.IsStatic)
        {
            ArgumentNullException.ThrowIfNull(target);
            if (method.DeclaringType == null || !method.DeclaringType.IsInstanceOfType(target))
            {
                throw new ArgumentException(
                    $"Function target must be an instance of {method.DeclaringType?.FullName ?? "the declaring type"}.",
                    nameof(target));
            }
        }

        var function = AIFunctionFactory.Create(method, target, name, description);
        return parameterInfos == null || parameterInfos.Count == 0
            ? function
            : new FunctionRenderSchemaAIFunction(function, method, parameterInfos);
    }

    private sealed class FunctionRenderSchemaAIFunction : DelegatingAIFunction
    {
        private readonly JsonElement _jsonSchema;
        private readonly string _requestParameterName;
        private readonly HashSet<string> _requestPropertyNames;

        public FunctionRenderSchemaAIFunction(
            AIFunction innerFunction,
            MethodInfo method,
            IReadOnlyList<FunctionParameterInfo> parameterInfos)
            : base(innerFunction)
        {
            var schema = FunctionRenderAiSchemaBuilder.ApplySelectionMetadata(
                innerFunction.JsonSchema,
                parameterInfos);
            _jsonSchema = FlattenRequestSchema(schema, method);
            var parameters = method.GetParameters();
            var requestParameter = parameters.Length == 1 ? parameters[0] : null;
            _requestParameterName = requestParameter?.Name ?? "request";
            _requestPropertyNames = parameterInfos
                .Where(parameter => !string.IsNullOrWhiteSpace(parameter?.Name))
                .Select(parameter => parameter.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public override JsonElement JsonSchema => _jsonSchema;

        protected override ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            if (arguments != null
                && !ContainsArgument(arguments, _requestParameterName)
                && arguments.Keys.Any(key => _requestPropertyNames.Contains(key)))
            {
                var requestProperties = arguments.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value,
                    StringComparer.OrdinalIgnoreCase);
                var normalizedArguments = new AIFunctionArguments(
                    new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        [_requestParameterName] = requestProperties
                    })
                {
                    Services = arguments.Services,
                    Context = arguments.Context
                };
                return base.InvokeCoreAsync(normalizedArguments, cancellationToken);
            }

            return base.InvokeCoreAsync(arguments, cancellationToken);
        }

        /// <summary>
        /// Determines whether an AI argument collection contains a named argument
        /// using case-insensitive matching.
        /// </summary>
        /// <param name="arguments">The AI function arguments.</param>
        /// <param name="name">The argument name to find.</param>
        /// <returns><see langword="true"/> when the name exists.</returns>
        private static bool ContainsArgument(AIFunctionArguments arguments, string name) =>
            arguments.Keys.Any(key => string.Equals(key, name, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Flattens a single request-object parameter schema so its properties are
        /// presented directly to AdminChat while invocation can still rebuild the request object.
        /// </summary>
        /// <param name="schema">The original function JSON Schema.</param>
        /// <param name="method">The reflected function method.</param>
        /// <returns>The flattened schema, or a clone of the original when flattening is not applicable.</returns>
        private static JsonElement FlattenRequestSchema(JsonElement schema, MethodInfo method)
        {
            var parameters = method.GetParameters();
            var requestParameter = parameters.Length == 1 ? parameters[0] : null;
            if (requestParameter == null ||
                schema.ValueKind != JsonValueKind.Object ||
                JsonNode.Parse(schema.GetRawText()) is not JsonObject root ||
                !TryGetObjectProperty(root, "properties", out var properties) ||
                !TryGetObjectProperty(properties, requestParameter.Name, out var requestSchema) ||
                !TryGetObjectProperty(requestSchema, "properties", out _))
            {
                return schema.Clone();
            }

            using var document = JsonDocument.Parse(requestSchema.ToJsonString());
            return document.RootElement.Clone();
        }

        /// <summary>
        /// Gets a JSON object property using case-insensitive matching.
        /// </summary>
        /// <param name="parent">The JSON object to inspect.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">Receives the nested object value.</param>
        /// <returns><see langword="true"/> when a matching object property exists.</returns>
        private static bool TryGetObjectProperty(
            JsonObject parent,
            string propertyName,
            out JsonObject value)
        {
            foreach (var property in parent)
            {
                if (string.Equals(property.Key, propertyName, StringComparison.OrdinalIgnoreCase) &&
                    property.Value is JsonObject objectValue)
                {
                    value = objectValue;
                    return true;
                }
            }

            value = null;
            return false;
        }
    }
}
