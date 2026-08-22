/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkflowObjectContracts.cs
    文件功能描述：NeuChar Workflow 面向其他 XNCF 模块的受控对象契约


    创建标识：Senparc - 20260810

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 扩展工作流模块的对象与事件契约

    修改标识：Senparc - 20260817
    修改描述：v0.2.0 扩展 Human Input 与工作流对象契约

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

/// <summary>
/// Workflow 向对象 Provider 传递的通用执行策略键。具体枚举和值域由 Provider 校验，
/// Workflow 不依赖实现模块的领域类型。
/// </summary>
public static class WorkflowObjectExecutionParameters
{
    public const string AllowFunctionCalls = "allowFunctionCalls";
    public const string HumanInTheLoopLevel = "humanInTheLoopLevel";
    public const string PluginToolPermission = "pluginToolPermission";
    public const string McpToolPermission = "mcpToolPermission";
    public const string IncludeHumanParticipant = "includeHumanParticipant";
    public const string ChatMaxRound = "chatMaxRound";
    public const string Personality = "personality";
}

/// <summary>
/// 受控对象执行后可供 Workflow Console 和回看页使用的外部详情引用。
/// Output 保持为节点可继续传递的业务结果，详情引用不参与下游模板计算。
/// </summary>
public sealed record WorkflowObjectExecutionReference(
    string Kind,
    string ProviderId,
    int? ChatTaskId = null,
    int? ChatGroupId = null,
    string DisplayName = null);

public sealed record WorkflowObjectExecutionResult(
    bool Success,
    string Output,
    string ErrorMessage = null,
    WorkflowObjectExecutionReference Reference = null);

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
