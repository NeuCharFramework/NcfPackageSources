/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: WorkflowEventPublisher.cs
    File description: 增强工作流编排、回放、Webhook 与并行执行能力


    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Application.Events;

/// <summary>
/// Workflow 只发布状态通知。IEventBus 将事件写入 Channel 即返回，调用方不等待订阅模块执行，
/// 因此保存、运行和调度线程不会被跨模块消费者阻塞。
/// </summary>
public sealed class WorkflowEventPublisher
{
    private readonly IEventBus _eventBus;

    public WorkflowEventPublisher(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public ValueTask PublishAsync(int workflowId, string changeType, int adminUserId,
        CancellationToken cancellationToken = default) =>
        _eventBus.PublishAsync(
            new WorkflowChangedEvent(workflowId, changeType, adminUserId, DateTimeOffset.UtcNow),
            cancellationToken);
}
