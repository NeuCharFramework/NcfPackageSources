/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkflowObjectContracts.cs
    文件功能描述：NeuChar Workflow 面向其他 XNCF 模块的受控对象契约


    创建标识：Senparc - 20260810

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 扩展工作流模块的对象与事件契约

----------------------------------------------------------------*/

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;

/// <summary>
/// 可被 Workflow 发现和调用的对象。该契约不提供任意 CLR 方法执行能力。
/// </summary>
public sealed record WorkflowObjectDescriptor(
    string ProviderId,
    string ObjectId,
    string Kind,
    string Name,
    string Description,
    bool Enabled,
    string Icon = null,
    string EditUrl = null,
    IReadOnlyDictionary<string, string> Metadata = null);

public sealed record WorkflowObjectExecutionRequest(
    string ObjectId,
    string Input,
    int AiModelId,
    string CorrelationId,
    IReadOnlyDictionary<string, string> Parameters = null,
    int? AdminUserId = null);

public sealed record WorkflowObjectExecutionResult(
    bool Success,
    string Output,
    string ErrorMessage = null);

/// <summary>
/// 外部 XNCF 模块实现此接口，将其受控对象提供给已安装且开启的 Workflow 模块。
/// 每次查询与执行都必须再次校验自身模块和目标对象的启用状态。
/// </summary>
public interface IWorkflowObjectProvider
{
    string ProviderId { get; }

    ValueTask<IReadOnlyList<WorkflowObjectDescriptor>> GetObjectsAsync(
        CancellationToken cancellationToken = default);

    ValueTask<WorkflowObjectExecutionResult> ExecuteAsync(
        WorkflowObjectExecutionRequest request,
        CancellationToken cancellationToken = default);
}
