/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowFunctionCallingProvider.cs
    文件功能描述：将已启用 Workflow 暴露为宿主可选的 Function Calling 工具

    修改标识：Senparc - 20260822
    修改描述：v0.2.0 增强工作流函数调用、任务控制与回放管理

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.Xncf.NeuCharWorkflow.Application.AppServices;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WorkflowEntity = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>
/// Workflow 模块安装且开放时才提供服务；每次执行仍由应用服务重新校验归属、启用状态和图引用。
/// </summary>
public sealed class NeuCharWorkflowFunctionCallingProvider : IWorkflowFunctionCallingProvider
{
    private readonly NeuCharWorkflowService _workflowService;
    private readonly NeuCharWorkflowAppService _workflowAppService;
    private readonly XncfModuleService _moduleService;
    private readonly IServiceScopeFactory _scopeFactory;

    public NeuCharWorkflowFunctionCallingProvider(
        NeuCharWorkflowService workflowService,
        NeuCharWorkflowAppService workflowAppService,
        XncfModuleService moduleService,
        IServiceScopeFactory scopeFactory)
    {
        _workflowService = workflowService;
        _workflowAppService = workflowAppService;
        _moduleService = moduleService;
        _scopeFactory = scopeFactory;
    }

    public async Task<IReadOnlyList<WorkflowFunctionCallingDescriptor>> GetAvailableAsync(
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (adminUserId <= 0 || !await IsModuleOpenAsync().ConfigureAwait(false))
        {
            return Array.Empty<WorkflowFunctionCallingDescriptor>();
        }

        var workflows = await _workflowService.GetFullListAsync(
            workflow => workflow.AdminUserId == adminUserId && workflow.Enabled,
            workflow => workflow.Name,
            OrderingType.Ascending).ConfigureAwait(false);

        return workflows
            .Select(workflow => new WorkflowFunctionCallingDescriptor(
                workflow.Id,
                workflow.Name,
                workflow.Description,
                GetParameters(workflow)))
            .ToList();
    }

    public async Task<WorkflowFunctionCallingResult> ExecuteAsync(
        int workflowId,
        int adminUserId,
        string input,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        // Agent Function invocations can resume after an external HIL request and may
        // overlap other Group/Workflow work. Do not reuse the scoped provider captured
        // while the Agent tools were built; its services own a scoped EF Core DbContext.
        using var scope = _scopeFactory.CreateScope();
        var isolatedProvider = scope.ServiceProvider
            .GetRequiredService<NeuCharWorkflowFunctionCallingProvider>();
        return await isolatedProvider.ExecuteCoreAsync(
                workflowId,
                adminUserId,
                input,
                parameters,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<WorkflowFunctionCallingResult> ExecuteCoreAsync(
        int workflowId,
        int adminUserId,
        string input,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        if (!await IsModuleOpenAsync().ConfigureAwait(false))
        {
            return Failure("NeuChar Workflow 模块未安装或未开启。");
        }

        var workflow = await _workflowService.GetObjectAsync(workflow =>
            workflow.Id == workflowId &&
            workflow.AdminUserId == adminUserId &&
            workflow.Enabled).ConfigureAwait(false);
        if (workflow == null)
        {
            return Failure("工作流不存在、未启用，或没有当前管理员的访问权限。");
        }

        var normalizedParameters = parameters?
            .Where(pair => !string.Equals(pair.Key, "input", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var hasParameters = normalizedParameters.Count > 0;
        var workflowInput = hasParameters
            ? JsonSerializer.Serialize(
                normalizedParameters
                    .Prepend(new KeyValuePair<string, object?>("input", input ?? string.Empty))
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase))
            : input ?? string.Empty;

        try
        {
            var result = await _workflowAppService.RunImmediatelyAsync(
                workflow.Id,
                adminUserId,
                workflowInput,
                cancellationToken,
                parseInputAsJson: hasParameters).ConfigureAwait(false);
            return result.Success
                ? new WorkflowFunctionCallingResult(true, result.Output ?? string.Empty, null)
                : Failure(result.ErrorMessage ?? "工作流执行失败。");
        }
        catch (Exception ex)
        {
            return Failure(ex.Message);
        }
    }

    private async Task<bool> IsModuleOpenAsync()
    {
        var module = await _moduleService.GetObjectAsync(module => module.Uid == new Register().Uid)
            .ConfigureAwait(false);
        return module?.State == XncfModules_State.开放;
    }

    private static IReadOnlyList<WorkflowFunctionCallingParameter> GetParameters(WorkflowEntity workflow)
    {
        if (!string.Equals(workflow.TriggerType, "webhook", StringComparison.OrdinalIgnoreCase))
        {
            return Array.Empty<WorkflowFunctionCallingParameter>();
        }

        try
        {
            return NeuCharWorkflowWebhookConfig.ParseStored(workflow.TriggerConfigJson)
                .Parameters
                .Where(parameter => !string.Equals(parameter.Name, "input", StringComparison.OrdinalIgnoreCase))
                .Select(parameter => new WorkflowFunctionCallingParameter(parameter.Name, parameter.Description))
                .ToList();
        }
        catch (InvalidOperationException)
        {
            return Array.Empty<WorkflowFunctionCallingParameter>();
        }
    }

    private static WorkflowFunctionCallingResult Failure(string message) =>
        new(false, null, message);
}
