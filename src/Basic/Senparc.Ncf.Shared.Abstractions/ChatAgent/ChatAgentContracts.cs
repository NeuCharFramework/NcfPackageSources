/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatAgentContracts.cs
    File description: Related implementation.

    Created by: Senparc - 20260809

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.7.1-preview2 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.7.1.2-preview3 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.ChatAgent;

/// <summary>
/// Identifies the schema-generation operation requested from a Chat Agent provider.
/// </summary>
public enum ChatAgentOperation
{
    GenerateNeuCharPivot = 0,
    RefineNeuCharPivot = 1
}

/// <summary>
/// Requests a Chat Agent to generate or refine a NeuChar Pivot schema.
/// </summary>
/// <param name="Operation">The schema operation to perform.</param>
/// <param name="CallerModuleUid">The module issuing the request.</param>
/// <param name="TargetModuleUid">The module whose schema is being generated or refined.</param>
/// <param name="AdminUserId">The administrator who initiated the operation.</param>
/// <param name="AiModelId">The AI model identifier selected for the operation.</param>
/// <param name="UserRequirement">The natural-language requirement guiding the operation.</param>
/// <param name="CurrentSchemaJson">The current schema when refinement is requested.</param>
/// <param name="ChatSessionId">The optional chat session associated with the request.</param>
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
/// Returns the generated or refined schema produced by a Chat Agent request.
/// </summary>
/// <param name="RequestId">The correlated request identifier.</param>
/// <param name="Success">Whether the operation completed successfully.</param>
/// <param name="SchemaJson">The generated schema JSON when successful.</param>
/// <param name="ChatSessionId">The related chat session identifier.</param>
/// <param name="Message">The caller-facing status message.</param>
/// <param name="ErrorMessage">The failure message when unsuccessful.</param>
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
