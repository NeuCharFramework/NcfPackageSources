/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatAgentContracts.cs
    File description: Related implementation.

    Created by: Senparc - 20260809

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview5 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.ChatAgent;

/// <summary>
///Legacy comment translated to English.
/// </summary>
public enum ChatAgentOperation
{
    GenerateNeuCharPivot = 0,
    RefineNeuCharPivot = 1
}

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public sealed record ChatAgentRequestEvent(
    ChatAgentOperation Operation,
    string CallerModuleUid,
    string TargetModuleUid,
    int AdminUserId,
    int AiModelId,
    string UserRequirement,
    string CurrentSchemaJson = null,
    int? ChatSessionId = null) : IntegrationRequest<ChatAgentResponseEvent>
{
    public override string GetEventSummary() =>
        $"ChatAgentRequest[{RequestId:N}] Operation={Operation}, Target={TargetModuleUid}";
}

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed record ChatAgentResponseEvent(
    Guid RequestId,
    bool Success,
    string SchemaJson,
    int? ChatSessionId,
    string Message,
    string ErrorMessage = null) : IntegrationResponse(RequestId)
{
    public override string GetEventSummary() =>
        $"ChatAgentResponse[{RequestId:N}] Success={Success}";
}
