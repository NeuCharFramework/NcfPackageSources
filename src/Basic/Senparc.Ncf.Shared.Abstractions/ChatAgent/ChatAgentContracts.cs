/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ChatAgentContracts.cs
    文件功能描述：系统级 ChatAgent、NeuCharPivot 与 Workflow 公共契约
----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.ChatAgent;

/// <summary>
/// ChatAgent 可公开给扩展模块的操作。契约刻意不包含任意方法调用能力。
/// </summary>
public enum ChatAgentOperation
{
    GenerateNeuCharPivot = 0,
    RefineNeuCharPivot = 1
}

/// <summary>
/// 通过 EventBus 请求 ChatAgent 生成声明式界面。
/// AI 输出只能作为待校验的 JSON schema，不能包含或执行脚本、样式和 CLR 方法名。
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
/// ChatAgent 生成结果。SchemaJson 已在服务端经过白名单规范化。
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
