/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: IChatTool.cs
    File description: Provides ic hatt ool functionality.

    Created by: Senparc - 20261123

    Modified by: Senparc - 20261124
    Change description: v0.29.1 Expanded chat tool contracts and registry support for agent workflows.

    Modified by: Senparc - 20261125
    Change description: Added decoupled command parameters and option providers.

    Modified by: Senparc - 20261127
    Change description: v0.29.1.2 Extend shared chat-tool contracts for admin task initiation.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.ChatTools
{
    public interface IChatTool
    {
        IReadOnlyCollection<ChatToolDefinition> GetChatTools();
    }

    public interface IChatToolParameterOptionsProvider
    {
        Task<IReadOnlyCollection<ChatToolParameterOption>> GetOptionsAsync(
            ChatToolParameterOptionsRequest request,
            CancellationToken cancellationToken = default);
    }

    public interface IChatToolAsyncActionMonitor
    {
        bool Supports(ChatToolAsyncAction action);

        Task<ChatToolAsyncActionState> GetStateAsync(
            ChatToolAsyncAction action,
            CancellationToken cancellationToken = default);
    }

    public sealed class ChatToolDefinition
    {
        public string Command { get; init; }
        public string Label { get; init; }
        public string Hint { get; init; }
        public string ModuleName { get; init; }
        public string ModuleUid { get; init; }
        public string Instructions { get; init; }
        public IReadOnlyCollection<ChatToolFunctionDefinition> Functions { get; init; }
            = Array.Empty<ChatToolFunctionDefinition>();
        public IReadOnlyCollection<ChatToolParameterDefinition> Parameters { get; init; }
            = Array.Empty<ChatToolParameterDefinition>();
    }

    public sealed class ChatToolFunctionDefinition
    {
        public Type DeclaringType { get; init; }
        public string MethodName { get; init; }
    }

    public sealed class ChatToolParameterDefinition
    {
        public string Name { get; init; }
        public string Label { get; init; }
        public string Hint { get; init; }
        public string Placeholder { get; init; }
        public string Prefix { get; init; }
        public bool Required { get; init; }
        public bool IsPositional { get; init; }
        public int Order { get; init; }
        public bool AllowFreeform { get; init; } = true;
        public Type OptionsProviderType { get; init; }
        public IReadOnlyCollection<ChatToolParameterOption> Options { get; init; }
            = Array.Empty<ChatToolParameterOption>();
    }

    public sealed class ChatToolParameterOption
    {
        public string Value { get; init; }
        public string Label { get; init; }
        public string Description { get; init; }
        public string Keywords { get; init; }
    }

    public sealed class ChatToolParameterOptionsRequest
    {
        public string Command { get; init; }
        public string Parameter { get; init; }
        public string Query { get; init; }
        public int Limit { get; init; } = 50;
    }

    public sealed class ChatToolParameterOptionsResult
    {
        public string Command { get; init; }
        public string Parameter { get; init; }
        public IReadOnlyCollection<ChatToolParameterOption> Options { get; init; }
            = Array.Empty<ChatToolParameterOption>();
    }

    public sealed record ChatToolAsyncAction(
        string ActionType,
        string Target,
        int OwnerId,
        string Name);

    public sealed record ChatToolAsyncActionState(
        string ActionType,
        string Target,
        string Status,
        bool IsTerminal,
        string Message,
        string Result,
        int PendingHumanCount = 0);
}
