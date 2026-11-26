/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: ChatToolRegistry.cs
    File description: Provides c hatt oolr egistry functionality.

    Created by: Senparc - 20261123

    Modified by: Senparc - 20261124
    Change description: v0.29.1 Expanded chat tool contracts and registry support for agent workflows.

    Modified by: Senparc - 20261125
    Change description: Added command parameter metadata and option resolution.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace Senparc.Ncf.XncfBase.ChatTools
{
    public static class ChatToolRegistry
    {
        /// <summary>
        /// Collects valid chat tools from all registered chat-tool providers.
        /// </summary>
        /// <returns>Distinct tools ordered by module and command.</returns>
        public static IReadOnlyCollection<ChatToolDefinition> GetChatTools()
        {
            return GetChatTools(XncfRegisterManager.RegisterList.OfType<IChatTool>());
        }

        /// <summary>
        /// Collects, validates, de-duplicates, and orders tools from supplied providers.
        /// </summary>
        /// <param name="providers">The chat-tool providers to inspect.</param>
        /// <returns>Distinct valid tool definitions.</returns>
        public static IReadOnlyCollection<ChatToolDefinition> GetChatTools(
            IEnumerable<IChatTool> providers)
        {
            return (providers ?? Array.Empty<IChatTool>())
                .SelectMany(provider => provider.GetChatTools() ?? Array.Empty<ChatToolDefinition>())
                .Where(IsValid)
                .GroupBy(tool => tool.Command, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(tool => tool.ModuleName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(tool => tool.Command, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        /// <summary>
        /// Matches slash-command tools against a message prefix.
        /// </summary>
        /// <param name="message">The message whose leading command should be matched.</param>
        /// <returns>Matching tool definitions, or an empty collection.</returns>
        public static IReadOnlyCollection<ChatToolDefinition> Match(string message)
        {
            return Match(message, GetChatTools());
        }

        /// <summary>
        /// Matches a message command against a supplied tool collection.
        /// </summary>
        /// <param name="message">The message whose leading command should be matched.</param>
        /// <param name="tools">The tool definitions to search.</param>
        /// <returns>Matching tool definitions, or an empty collection.</returns>
        public static IReadOnlyCollection<ChatToolDefinition> Match(
            string message,
            IEnumerable<ChatToolDefinition> tools)
        {
            var value = (message ?? string.Empty).TrimStart();
            if (!value.StartsWith("/", StringComparison.Ordinal))
            {
                return Array.Empty<ChatToolDefinition>();
            }

            var separator = value.IndexOfAny(new[] { ' ', '\t', '\r', '\n' });
            var command = separator < 0 ? value : value[..separator];
            return (tools ?? Array.Empty<ChatToolDefinition>())
                .Where(tool => string.Equals(tool.Command, command, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        /// <summary>
        /// Resolves parameter options using the globally registered tool definitions.
        /// </summary>
        /// <param name="serviceProvider">The provider used to resolve dynamic option providers.</param>
        /// <param name="request">The command, parameter, query, and result limit.</param>
        /// <param name="cancellationToken">Token used to cancel dynamic option lookup.</param>
        /// <returns>A bounded option result.</returns>
        public static async Task<ChatToolParameterOptionsResult> GetParameterOptionsAsync(
            IServiceProvider serviceProvider,
            ChatToolParameterOptionsRequest request,
            CancellationToken cancellationToken = default)
        {
            return await GetParameterOptionsAsync(
                serviceProvider,
                request,
                GetChatTools(),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Resolves parameter options using a supplied tool collection.
        /// </summary>
        /// <param name="serviceProvider">The provider used to resolve dynamic option providers.</param>
        /// <param name="request">The command, parameter, query, and result limit.</param>
        /// <param name="tools">The tool definitions to search.</param>
        /// <param name="cancellationToken">Token used to cancel dynamic option lookup.</param>
        /// <returns>A bounded option result.</returns>
        public static async Task<ChatToolParameterOptionsResult> GetParameterOptionsAsync(
            IServiceProvider serviceProvider,
            ChatToolParameterOptionsRequest request,
            IEnumerable<ChatToolDefinition> tools,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);
            ArgumentNullException.ThrowIfNull(request);

            var tool = Match(request.Command, tools).FirstOrDefault();
            if (tool == null)
            {
                throw new ArgumentException($"Unknown chat command: {request.Command}", nameof(request));
            }

            var parameter = tool.Parameters
                .FirstOrDefault(item => string.Equals(item.Name, request.Parameter, StringComparison.OrdinalIgnoreCase));
            if (parameter == null)
            {
                throw new ArgumentException(
                    $"Unknown parameter '{request.Parameter}' for command '{tool.Command}'.",
                    nameof(request));
            }

            var limit = Math.Clamp(request.Limit <= 0 ? 50 : request.Limit, 1, 100);
            IReadOnlyCollection<ChatToolParameterOption> options;
            if (parameter.OptionsProviderType != null)
            {
                if (!typeof(IChatToolParameterOptionsProvider).IsAssignableFrom(parameter.OptionsProviderType))
                {
                    throw new InvalidOperationException(
                        $"{parameter.OptionsProviderType.FullName} must implement {nameof(IChatToolParameterOptionsProvider)}.");
                }

                var provider = serviceProvider.GetService(parameter.OptionsProviderType)
                    as IChatToolParameterOptionsProvider
                    ?? throw new InvalidOperationException(
                        $"Chat command option provider {parameter.OptionsProviderType.FullName} is not registered.");
                options = await provider.GetOptionsAsync(
                    new ChatToolParameterOptionsRequest
                    {
                        Command = tool.Command,
                        Parameter = parameter.Name,
                        Query = request.Query?.Trim(),
                        Limit = limit
                    },
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                options = parameter.Options ?? Array.Empty<ChatToolParameterOption>();
                var query = request.Query?.Trim();
                if (!string.IsNullOrWhiteSpace(query))
                {
                    options = options.Where(option =>
                            Contains(option.Value, query)
                            || Contains(option.Label, query)
                            || Contains(option.Description, query)
                            || Contains(option.Keywords, query))
                        .ToArray();
                }
            }

            return new ChatToolParameterOptionsResult
            {
                Command = tool.Command,
                Parameter = parameter.Name,
                Options = (options ?? Array.Empty<ChatToolParameterOption>())
                    .Where(IsValid)
                    .Take(limit)
                    .ToArray()
            };
        }

        public static IReadOnlyList<ChatToolAsyncAction> ExtractAsyncActions(object result)
        {
            if (result == null)
            {
                return Array.Empty<ChatToolAsyncAction>();
            }

            var actions = new List<ChatToolAsyncAction>();
            try
            {
                var json = result is string text
                    ? text
                    : JsonSerializer.Serialize(result);
                using var document = JsonDocument.Parse(json);
                ExtractAsyncActions(document.RootElement, actions);
                if (actions.Count == 0 && document.RootElement.ValueKind == JsonValueKind.String)
                {
                    var nested = document.RootElement.GetString();
                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        using var nestedDocument = JsonDocument.Parse(nested);
                        ExtractAsyncActions(nestedDocument.RootElement, actions);
                    }
                }
            }
            catch (JsonException)
            {
                return Array.Empty<ChatToolAsyncAction>();
            }

            return actions
                .GroupBy(action => $"{action.ActionType}:{action.Target}", StringComparer.Ordinal)
                .Select(group => group.First())
                .ToArray();
        }

        /// <summary>
        /// Validates command, label, function, parameter, and options-provider metadata.
        /// </summary>
        /// <param name="tool">The tool definition.</param>
        /// <returns><see langword="true"/> when the tool is safe to expose.</returns>
        private static bool IsValid(ChatToolDefinition tool)
        {
            return tool != null
                   && !string.IsNullOrWhiteSpace(tool.Command)
                   && tool.Command.StartsWith("/", StringComparison.Ordinal)
                   && !string.IsNullOrWhiteSpace(tool.Label)
                   && tool.Functions != null
                   && tool.Functions.All(function =>
                       function != null
                       && function.DeclaringType != null
                       && !string.IsNullOrWhiteSpace(function.MethodName))
                   && (tool.Parameters == null
                       || tool.Parameters.All(parameter =>
                           parameter != null
                           && !string.IsNullOrWhiteSpace(parameter.Name)
                           && (parameter.IsPositional || !string.IsNullOrWhiteSpace(parameter.Prefix))
                           && (parameter.OptionsProviderType == null
                               || typeof(IChatToolParameterOptionsProvider).IsAssignableFrom(parameter.OptionsProviderType))));
        }

        /// <summary>
        /// Validates one parameter option's required display/value fields.
        /// </summary>
        /// <param name="option">The option to validate.</param>
        /// <returns><see langword="true"/> when the option can be exposed.</returns>
        private static bool IsValid(ChatToolParameterOption option)
        {
            return option != null
                   && !string.IsNullOrWhiteSpace(option.Value)
                   && !string.IsNullOrWhiteSpace(option.Label);
        }

        /// <summary>
        /// Performs a case-insensitive substring match against an option field.
        /// </summary>
        /// <param name="value">The candidate field value.</param>
        /// <param name="query">The query text.</param>
        /// <returns><see langword="true"/> when the value contains the query.</returns>
        private static bool Contains(string value, string query)
        {
            return value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
        }

        private static void ExtractAsyncActions(
            JsonElement element,
            List<ChatToolAsyncAction> actions)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        ExtractAsyncActions(item, actions);
                    }
                    break;
                case JsonValueKind.String:
                    var text = element.GetString();
                    if (string.IsNullOrWhiteSpace(text)
                        || !text.Contains("admin-chat-action", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                    try
                    {
                        using var embedded = JsonDocument.Parse(text);
                        ExtractAsyncActions(embedded.RootElement, actions);
                    }
                    catch (JsonException)
                    {
                    }
                    break;
                case JsonValueKind.Object:
                    var actionType = GetString(element, "type");
                    if (!string.IsNullOrWhiteSpace(actionType)
                        && element.TryGetProperty("task", out var task))
                    {
                        var target = actionType.Equals("agents-task", StringComparison.OrdinalIgnoreCase)
                            ? GetInt32(task, "ChatTaskId").ToString()
                            : GetString(task, "RunId");
                        var ownerId = actionType.Equals("workflow-run", StringComparison.OrdinalIgnoreCase)
                            ? GetInt32(task, "WorkflowId")
                            : 0;
                        var name = actionType.Equals("agents-task", StringComparison.OrdinalIgnoreCase)
                            ? GetString(task, "ChatGroupName")
                            : GetString(task, "WorkflowName");
                        if (!string.IsNullOrWhiteSpace(target) && target != "0")
                        {
                            actions.Add(new ChatToolAsyncAction(actionType, target, ownerId, name));
                        }
                    }
                    foreach (var property in element.EnumerateObject())
                    {
                        if (!property.Name.Equals("task", StringComparison.OrdinalIgnoreCase))
                        {
                            ExtractAsyncActions(property.Value, actions);
                        }
                    }
                    break;
            }
        }

        private static string GetString(JsonElement element, string name)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (!property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                return property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()
                    : property.Value.ToString();
            }
            return null;
        }

        private static int GetInt32(JsonElement element, string name)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                    && property.Value.TryGetInt32(out var value))
                {
                    return value;
                }
            }
            return 0;
        }
    }
}
