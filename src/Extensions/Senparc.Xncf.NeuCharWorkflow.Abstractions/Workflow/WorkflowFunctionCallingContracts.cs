/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkflowFunctionCallingContracts.cs
    文件功能描述：WorkflowFunctionCallingContracts.cs 相关实现


    创建标识：Senparc - 20260821

    修改标识：Senparc - 20260822
    修改描述：v0.2.0 扩展工作流依赖、函数调用和人工交互契约

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;

/// <summary>
/// Optional integration boundary used by hosts that want to expose enabled Workflows
/// as AI function-calling tools.
/// </summary>
public interface IWorkflowFunctionCallingProvider
{
    Task<IReadOnlyList<WorkflowFunctionCallingDescriptor>> GetAvailableAsync(
        int adminUserId,
        CancellationToken cancellationToken = default);

    Task<WorkflowFunctionCallingResult> ExecuteAsync(
        int workflowId,
        int adminUserId,
        string input,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);
}

public sealed record WorkflowFunctionCallingDescriptor(
    int Id,
    string Name,
    string? Description,
    IReadOnlyList<WorkflowFunctionCallingParameter> Parameters);

/// <summary>
/// Workflow trigger parameters are intentionally optional in function-calling.
/// The normal <c>input</c> argument remains the stable required entry point.
/// </summary>
public sealed record WorkflowFunctionCallingParameter(
    string Name,
    string? Description);

public sealed record WorkflowFunctionCallingResult(
    bool Success,
    string? Output,
    string? ErrorMessage);
