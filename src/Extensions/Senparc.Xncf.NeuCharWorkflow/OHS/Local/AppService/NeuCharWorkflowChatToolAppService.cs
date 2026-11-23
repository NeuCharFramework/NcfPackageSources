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
                workflow.Id,
                WorkflowName = workflow.Name,
                RunId = runId,
                Status = "started"
            }));
            return logger.GetLogs();
        });
    }

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
