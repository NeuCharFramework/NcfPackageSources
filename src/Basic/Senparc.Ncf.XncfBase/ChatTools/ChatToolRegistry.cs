/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: ChatToolRegistry.cs
    File description: Provides c hatt oolr egistry functionality.

    Created by: Senparc - 20261123

    Modified by: Senparc - 20261124
    Change description: v0.29.1 Expanded chat tool contracts and registry support for agent workflows.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;

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
                       && !string.IsNullOrWhiteSpace(function.MethodName));
        }
    }
}
