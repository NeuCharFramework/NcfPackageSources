/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: IChatTool.cs
    File description: Provides ic hatt ool functionality.

    Created by: Senparc - 20261123

    Modified by: Senparc - 20261124
    Change description: v0.29.1 Expanded chat tool contracts and registry support for agent workflows.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;

namespace Senparc.Ncf.XncfBase.ChatTools
{
    public interface IChatTool
    {
        IReadOnlyCollection<ChatToolDefinition> GetChatTools();
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
    }

    public sealed class ChatToolFunctionDefinition
    {
        public Type DeclaringType { get; init; }
        public string MethodName { get; init; }
    }
}
