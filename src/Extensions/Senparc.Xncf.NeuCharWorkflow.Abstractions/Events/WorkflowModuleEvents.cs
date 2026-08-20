/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkflowModuleEvents.cs
    文件功能描述：NeuChar Workflow 异步模块通知契约


    创建标识：Senparc - 20260810

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 扩展工作流模块的对象与事件契约

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Events;
using System;

namespace Senparc.Xncf.NeuCharWorkflow.Abstractions.Events;

/// <summary>
/// Workflow 以发布通知的方式告知其他模块状态已变化。事件不承载同步执行结果，
/// 消费方不得阻塞工作流保存、运行或调度循环。
/// </summary>
public sealed record WorkflowChangedEvent(
    int WorkflowId,
    string ChangeType,
    int AdminUserId,
    DateTimeOffset OccurredAt) : IntegrationEvent
{
    public override string GetEventSummary() =>
        $"WorkflowChanged[{WorkflowId}] {ChangeType}";
}
