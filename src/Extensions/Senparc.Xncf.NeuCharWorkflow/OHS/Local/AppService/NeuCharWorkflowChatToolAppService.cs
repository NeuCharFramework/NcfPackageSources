/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowChatToolAppService.cs
    File description: Provides n euc harw orkflowc hatt oola pps ervice functionality.

    Created by: Senparc - 20261123

    Modified by: Senparc - 20261124
    Change description: v0.4.8 Enhanced workflow chat tools, editing commands, navigation, and localization behavior.

    Modified by: Senparc - 20261127
    Change description: v0.5.0.0 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using System.Text.Json;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Xncf.NeuCharWorkflow.Application.AppServices;

namespace Senparc.Xncf.NeuCharWorkflow.OHS.Local.AppService;

[AdminAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
public sealed class NeuCharWorkflowChatToolAppService(
    IServiceProvider serviceProvider,
    NeuCharWorkflowAppService workflowAppService,
    IAdminWorkContextProvider adminWorkContextProvider) : AppServiceBase(serviceProvider)
{
    private const int MaxSearchItems = 20;
    private const int MaxGraphCharacters = 40_000;
    private readonly NeuCharWorkflowAppService _workflowAppService = workflowAppService;
    private readonly IAdminWorkContextProvider _adminWorkContextProvider = adminWorkContextProvider;

    /// <summary>
    /// Searches owner-scoped workflows and returns bounded metadata for AdminChat.
    /// </summary>
    /// <param name="request">The optional name/description query.</param>
    /// <returns>A function response containing matching workflow metadata.</returns>
    [FunctionRender(
        typeof(NeuCharWorkflowResource),
        "AdminChat.Function.SearchWorkflows",
        "AdminChat.Function.SearchWorkflowsDescription",
        typeof(Register))]
    public Task<StringAppResponse> SearchWorkflows(WorkflowChatSearchRequest request)
    {
        return this.GetStringResponseAsync(async (_, logger) =>
        {
            var userId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
            var query = request?.Query?.Trim() ?? string.Empty;
            var workflows = await _workflowAppService.GetListAsync(userId)
                .ConfigureAwait(false);
            var result = workflows
                .Where(workflow => string.IsNullOrWhiteSpace(query)
                                   || workflow.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                                   || workflow.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                .Take(MaxSearchItems)
                .Select(workflow => new
                {
                    workflow.Id,
                    workflow.Name,
                    workflow.Description,
                    workflow.Enabled,
                    workflow.TriggerType,
                    workflow.Revision,
                    workflow.LastRunAt,
                    workflow.LastSucceeded,
                    workflow.LastError
                });
            logger.Append(JsonSerializer.Serialize(new
            {
                Query = query,
                TotalAvailable = workflows.Count,
                Returned = result.Count(),
                Items = result
            }));
            return logger.GetLogs();
        });
    }

    /// <summary>
    /// Starts one owner-scoped workflow run from an AdminChat request.
    /// </summary>
    /// <param name="request">The workflow identifier and optional input.</param>
    /// <returns>A function response containing the started run identifier.</returns>
    [FunctionRender(
        typeof(NeuCharWorkflowResource),
        "AdminChat.Function.StartWorkflow",
        "AdminChat.Function.StartWorkflowDescription",
        typeof(Register))]
    public Task<StringAppResponse> StartWorkflow(WorkflowChatStartRequest request)
    {
        return this.GetStringResponseAsync(async (_, logger) =>
        {
            if (request?.WorkflowId <= 0)
            {
                return "WorkflowId must be greater than 0.";
            }

            var userId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
            var workflow = await _workflowAppService.GetDetailAsync(request.WorkflowId, userId)
                .ConfigureAwait(false)
                ?? throw new WorkflowNotFoundException();
            var runId = await _workflowAppService.StartRunAsync(
                    workflow.Id,
                    userId,
                    request.Input ?? string.Empty)
                .ConfigureAwait(false);
            logger.Append(JsonSerializer.Serialize(new
            {
                kind = "admin-chat-action",
                actions = new[]
                {
                    new
                    {
                        type = "workflow-run",
                        task = new
                        {
                            WorkflowId = workflow.Id,
                            WorkflowName = workflow.Name,
                            RunId = runId.ToString("N"),
                            Status = "started"
                        }
                    }
                }
            }));
            return logger.GetLogs();
        });
    }

    /// <summary>
    /// Returns a bounded read-only workflow inspection payload for AdminChat.
    /// </summary>
    /// <param name="request">The workflow identifier and graph size limit.</param>
    /// <returns>A function response containing workflow metadata and graph JSON.</returns>
    [FunctionRender(
        typeof(NeuCharWorkflowResource),
        "AdminChat.Function.InspectWorkflow",
        "AdminChat.Function.InspectWorkflowDescription",
        typeof(Register))]
    public Task<StringAppResponse> InspectWorkflow(WorkflowChatInspectRequest request)
    {
        return this.GetStringResponseAsync(async (_, logger) =>
        {
            if (request?.WorkflowId <= 0)
            {
                return "WorkflowId must be greater than 0.";
            }

            var userId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
            var workflow = await _workflowAppService.GetDetailAsync(request.WorkflowId, userId)
                .ConfigureAwait(false)
                ?? throw new WorkflowNotFoundException();
            var graph = workflow.GraphJson ?? string.Empty;
            var maxCharacters = Math.Clamp(
                request.MaxCharacters <= 0 ? 20_000 : request.MaxCharacters,
                2_000,
                MaxGraphCharacters);
            logger.Append(JsonSerializer.Serialize(new
            {
                workflow.Id,
                workflow.Name,
                workflow.Description,
                workflow.Enabled,
                workflow.TriggerType,
                workflow.Revision,
                workflow.AutoSaveMinutes,
                workflow.LastUpdateTime,
                GraphJson = graph.Length <= maxCharacters
                    ? graph
                    : graph[..maxCharacters] + "\n...[truncated]",
                Truncated = graph.Length > maxCharacters,
                Safety = "Read only. Propose a patch or instructions; never claim the workflow was saved."
            }));
            return logger.GetLogs();
        });
    }

    /// <summary>
    /// Applies a revision-checked graph edit to an owner-scoped workflow.
    /// </summary>
    /// <param name="request">The workflow identifier, expected revision, and replacement graph JSON.</param>
    /// <returns>A function response containing the persisted revision and save status.</returns>
    [FunctionRender(
        typeof(NeuCharWorkflowResource),
        "AdminChat.Function.ApplyWorkflowEdit",
        "AdminChat.Function.ApplyWorkflowEditDescription",
        typeof(Register))]
    public Task<StringAppResponse> ApplyWorkflowEdit(WorkflowChatApplyEditRequest request)
    {
        return this.GetStringResponseAsync(async (_, logger) =>
        {
            if (request?.WorkflowId <= 0)
            {
                return "WorkflowId must be greater than 0.";
            }
            if (string.IsNullOrWhiteSpace(request.GraphJson))
            {
                return "GraphJson is required.";
            }
            if (request.ExpectedRevision <= 0)
            {
                return "ExpectedRevision must come from InspectWorkflow.";
            }

            var userId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
            var workflow = await _workflowAppService.GetDetailAsync(request.WorkflowId, userId)
                .ConfigureAwait(false)
                ?? throw new WorkflowNotFoundException();
            if (workflow.Revision != request.ExpectedRevision)
            {
                return $"Revision conflict: expected {request.ExpectedRevision}, current {workflow.Revision}. Inspect the latest graph and retry.";
            }

            var saved = await _workflowAppService.SaveAsync(
                    new SaveWorkflowCommand(
                        workflow.Id,
                        workflow.Name,
                        workflow.Description,
                        request.GraphJson,
                        workflow.Enabled,
                        workflow.TriggerType,
                        workflow.TriggerConfigJson,
                        workflow.AutoSaveMinutes,
                        request.ExpectedRevision,
                        "admin-chat-ai"),
                    userId)
                .ConfigureAwait(false);
            logger.Append(JsonSerializer.Serialize(new
            {
                saved.Id,
                saved.Name,
                saved.Revision,
                Status = "saved",
                Instruction = "Reload the Workflow editor before making any further page edits."
            }));
            return logger.GetLogs();
        });
    }
}

public sealed class WorkflowChatSearchRequest
{
    public string? Query { get; set; }
}

public sealed class WorkflowChatStartRequest
{
    public int WorkflowId { get; set; }
    public string? Input { get; set; }
}

public sealed class WorkflowChatInspectRequest
{
    public int WorkflowId { get; set; }
    public int MaxCharacters { get; set; } = 20_000;
}

public sealed class WorkflowChatApplyEditRequest
{
    public int WorkflowId { get; set; }
    public string? GraphJson { get; set; }
    public int ExpectedRevision { get; set; }
}
