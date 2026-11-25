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

namespace Senparc.Ncf.XncfBase.ChatTools
{
    public static class ChatToolRegistry
    {
        public static IReadOnlyCollection<ChatToolDefinition> GetChatTools()
        {
            return GetChatTools(XncfRegisterManager.RegisterList.OfType<IChatTool>());
        }

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

        public static IReadOnlyCollection<ChatToolDefinition> Match(string message)
        {
            return Match(message, GetChatTools());
        }

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

        private static bool IsValid(ChatToolParameterOption option)
        {
            return option != null
                   && !string.IsNullOrWhiteSpace(option.Value)
                   && !string.IsNullOrWhiteSpace(option.Label);
        }

        private static bool Contains(string value, string query)
        {
            return value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
