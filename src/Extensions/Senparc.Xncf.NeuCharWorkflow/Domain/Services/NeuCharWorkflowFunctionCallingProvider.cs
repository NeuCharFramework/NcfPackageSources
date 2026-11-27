/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowFunctionCallingProvider.cs
    File description: Related implementation.

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance workflow function calls, task control, and replay management

    Modified by: Senparc - 20260829
    Change description: v0.3.0 add workflow analysis queries and admin-side visualization

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.5.0.0 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

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
/// Exposes enabled Workflow definitions as callable Agent functions and executes them
/// in an isolated dependency-injection scope.
/// </summary>
public sealed class NeuCharWorkflowFunctionCallingProvider : IWorkflowFunctionCallingProvider
{
    private readonly NeuCharWorkflowService _workflowService;
    private readonly NeuCharWorkflowAppService _workflowAppService;
    private readonly XncfModuleService _moduleService;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes the workflow function-calling provider and its scoped execution dependencies.
    /// </summary>
    /// <param name="workflowService">The workflow persistence service.</param>
    /// <param name="workflowAppService">The application service used to execute workflows.</param>
    /// <param name="moduleService">The XNCF module service used to verify module availability.</param>
    /// <param name="scopeFactory">The scope factory used to isolate execution dependencies.</param>
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

    /// <summary>
    /// Lists enabled workflows callable by the specified administrator.
    /// </summary>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="cancellationToken">Token used to cancel catalog loading.</param>
    /// <returns>Callable workflow descriptors, or an empty list when the module is unavailable.</returns>
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

    /// <summary>
    /// Executes one callable workflow in an isolated dependency-injection scope.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="input">The input text supplied to the workflow.</param>
    /// <param name="parameters">Additional named parameters.</param>
    /// <param name="cancellationToken">Token used to cancel execution.</param>
    /// <returns>The workflow function-calling result.</returns>
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

    /// <summary>
    /// Executes a workflow after checking module availability, owner access, enabled state,
    /// and parameter normalization.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="adminUserId">The administrator owner identifier.</param>
    /// <param name="input">The primary workflow input.</param>
    /// <param name="parameters">Additional named parameters.</param>
    /// <param name="cancellationToken">The execution cancellation token.</param>
    /// <returns>The workflow function-calling result.</returns>
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

    /// <summary>
    /// Determines whether the NeuCharWorkflow module is installed and currently open.
    /// </summary>
    /// <returns><see langword="true"/> when the module can execute workflows.</returns>
    private async Task<bool> IsModuleOpenAsync()
    {
        var module = await _moduleService.GetObjectAsync(module => module.Uid == new Register().Uid)
            .ConfigureAwait(false);
        return module?.State == XncfModules_State.开放;
    }

    /// <summary>
    /// Converts webhook trigger parameter metadata into callable function parameters.
    /// Non-webhook workflows and malformed trigger configuration expose no parameters.
    /// </summary>
    /// <param name="workflow">The workflow whose trigger configuration is inspected.</param>
    /// <returns>The callable parameter descriptors.</returns>
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

    /// <summary>
    /// Creates a failed workflow function-calling result.
    /// </summary>
    /// <param name="message">The user-facing failure message.</param>
    /// <returns>A failed result containing the message.</returns>
    private static WorkflowFunctionCallingResult Failure(string message) =>
        new(false, null, message);
}
