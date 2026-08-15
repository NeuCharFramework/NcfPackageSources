/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkflowHumanInteractionContracts.cs
    文件功能描述：Workflow 与外部人工交互模块之间的 HIL 桥接契约

    创建标识：Senparc - 20260815
    修改描述：Workflow 运行可读取并提交由 AgentsManager 托管的 Human 请求

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;

/// <summary>
/// Workflow 运行中由外部 Agent 执行器托管的人工交互请求。
/// Workflow 只消费契约，不依赖 AgentsManager 的实现与页面。
/// </summary>
public sealed record WorkflowHumanInteraction(
    string RequestId,
    int ChatTaskId,
    string CorrelationId,
    string RequestType,
    string AgentName,
    string ToolName,
    string ToolArguments,
    string Prompt,
    string ParticipantKey,
    string NeuBellItemId,
    DateTimeOffset CreatedAt);

public sealed record WorkflowHumanInteractionResult(
    bool Success,
    bool Approved,
    string Input,
    string Reason,
    string Message);

/// <summary>
/// Workflow 与外部人工交互执行器的最小桥接。
/// 实现方负责请求关联、权限校验、恢复执行以及提醒消费。
/// </summary>
public interface IWorkflowHumanInteractionBridge
{
    ValueTask<IReadOnlyList<WorkflowHumanInteraction>> GetPendingAsync(
        string correlationId,
        string userId,
        CancellationToken cancellationToken = default);

    ValueTask<WorkflowHumanInteractionResult> ResolveAsync(
        string correlationId,
        string userId,
        string requestId,
        bool approved,
        string input = null,
        string reason = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// AgentsManager 未安装时的安全空实现。
/// </summary>
public sealed class NullWorkflowHumanInteractionBridge : IWorkflowHumanInteractionBridge
{
    public ValueTask<IReadOnlyList<WorkflowHumanInteraction>> GetPendingAsync(
        string correlationId,
        string userId,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IReadOnlyList<WorkflowHumanInteraction>>(
            Array.Empty<WorkflowHumanInteraction>());

    public ValueTask<WorkflowHumanInteractionResult> ResolveAsync(
        string correlationId,
        string userId,
        string requestId,
        bool approved,
        string input = null,
        string reason = null,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new WorkflowHumanInteractionResult(
            false,
            false,
            null,
            null,
            "当前未安装支持 Workflow Human-in-the-Loop 的执行模块。"));
}
