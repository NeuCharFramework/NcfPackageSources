/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AgentExecutionAppService.cs
    文件功能描述：独立 Agent 执行任务管理与统一外部调用应用服务

    创建标识：Senparc - 20260822

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 统一独立 Agent 执行、列表、详情和用量入口

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 新增独立 Agent 执行任务持久化、管理页和 SSE 过程回放


----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.CO2NET.WebApi;
using Senparc.Xncf.AgentsManager.Application.DTOs;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AreaBase.Admin.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService;

[ApiAuthorize]
public sealed class AgentExecutionAppService : AppServiceBase
{
    private readonly AgentExecutionService _executionService;
    private readonly AgentsTemplateService _agentTemplateService;

    public AgentExecutionAppService(
        IServiceProvider serviceProvider,
        AgentExecutionService executionService,
        AgentsTemplateService agentTemplateService)
        : base(serviceProvider)
    {
        _executionService = executionService;
        _agentTemplateService = agentTemplateService;
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public async Task<AppResponseBase<AgentExecutionListResponse>> GetList(
        int agentTemplateId = 0,
        string source = "",
        string filter = "",
        int status = -1,
        int pageIndex = 0,
        int pageSize = 20)
    {
        return await this.GetResponseAsync<AgentExecutionListResponse>(async (_, _) =>
        {
            var statusFilter = Enum.IsDefined(typeof(AgentExecutionTask_Status), status)
                ? (AgentExecutionTask_Status?)status
                : null;
            var tasks = await _executionService.GetTaskDtosAsync(
                agentTemplateId,
                source,
                filter,
                statusFilter,
                Math.Max(0, pageIndex),
                Math.Clamp(pageSize, 1, 100));
            return new AgentExecutionListResponse { Tasks = tasks };
        });
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public async Task<AppResponseBase<AgentExecutionTaskDto>> GetItem(int id)
    {
        return await this.GetResponseAsync<AgentExecutionTaskDto>(async (response, _) =>
        {
            var task = await _executionService.GetTaskDtoAsync(id, includeEvents: true);
            if (task == null)
            {
                response.Success = false;
                response.ErrorMessage = "独立 Agent 执行任务不存在。";
            }

            return task;
        });
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public async Task<AppResponseBase<IReadOnlyList<AgentExecutionEventDto>>> GetEvents(
        int id,
        int afterSequence = 0)
    {
        return await this.GetResponseAsync<IReadOnlyList<AgentExecutionEventDto>>(async (_, _) =>
            await _executionService.GetEventsAsync(id, afterSequence));
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public async Task<AppResponseBase<IReadOnlyList<HumanInTheLoopRequestDto>>> GetHumanRequests(
        int agentExecutionTaskId)
    {
        return await this.GetResponseAsync<IReadOnlyList<HumanInTheLoopRequestDto>>((_, _) =>
            Task.FromResult<IReadOnlyList<HumanInTheLoopRequestDto>>(
                base.GetRequiredService<HumanInTheLoopRequestStore>()
                    .GetPendingForAgentExecution(agentExecutionTaskId)));
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<string>> ResolveHumanRequest(
        string requestId,
        bool approved,
        string reason = null)
    {
        return await this.GetResponseAsync<string>(async (response, _) =>
        {
            var resolution = await base.GetRequiredService<AgentsManagerHumanInteractionService>()
                .ResolveAsync(
                    requestId,
                    GetCurrentAdminUserId().ToString(),
                    new HumanInTheLoopDecision(approved, reason));
            if (!resolution.Success)
            {
                response.Success = false;
                response.ErrorMessage = resolution.Message;
                return null;
            }

            return approved ? "工具审批已批准，任务继续执行。" : "工具审批已拒绝，任务继续执行。";
        });
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public async Task<AppResponseBase<IReadOnlyList<AgentExecutionAgentOption>>> GetAgents()
    {
        return await this.GetResponseAsync<IReadOnlyList<AgentExecutionAgentOption>>(async (_, _) =>
        {
            var agents = await _agentTemplateService.GetFullListAsync(
                item => item.Enable && !item.IsHuman,
                item => item.Name,
                Ncf.Core.Enums.OrderingType.Ascending);
            return agents.Select(item => new AgentExecutionAgentOption(item.Id, item.Name)).ToList();
        });
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<AgentExecutionStartResponse>> Start(
        [FromBody] AgentExecutionStartRequest request)
    {
        return await this.GetResponseAsync<AgentExecutionStartResponse>(async (response, _) =>
        {
            var normalized = BuildRequest(request);
            var task = await _executionService.StartAsync(normalized);
            return new AgentExecutionStartResponse { Task = task };
        });
    }

    /// <summary>
    /// 同步执行入口。Workflow、内部模块和需要等待结果的外部调用可以使用它；
    /// 管理页默认使用 Start + SSE，避免 HTTP 请求被模型执行时间占用。
    /// </summary>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<AgentExecutionRunResponse>> Run(
        [FromBody] AgentExecutionStartRequest request,
        CancellationToken cancellationToken = default)
    {
        return await this.GetResponseAsync<AgentExecutionRunResponse>(async (_, _) =>
        {
            var result = await _executionService.ExecuteAsync(
                BuildRequest(request),
                cancellationToken);
            var task = await _executionService.GetTaskDtoAsync(result.TaskId, includeEvents: true);
            return new AgentExecutionRunResponse { Task = task, Result = result };
        });
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<string>> Cancel(int id)
    {
        return await this.GetResponseAsync<string>((response, _) =>
        {
            if (!_executionService.Cancel(id))
            {
                response.Success = false;
                response.ErrorMessage = "任务不存在、已完成，或当前进程无法取消该任务。";
                return Task.FromResult<string>(null);
            }

            return Task.FromResult("已请求取消独立 Agent 执行。");
        });
    }

    private AgentExecutionRequest BuildRequest(AgentExecutionStartRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.AgentTemplateId <= 0)
        {
            throw new InvalidOperationException("未选择有效的独立 Agent。");
        }

        return new AgentExecutionRequest
        {
            AgentTemplateId = request.AgentTemplateId,
            Name = request.Name,
            Input = request.Input,
            Source = string.IsNullOrWhiteSpace(request.Source) ? "Direct" : request.Source,
            CorrelationId = request.CorrelationId,
            ExternalReference = request.ExternalReference,
            WorkflowId = request.WorkflowId,
            AdminUserId = GetCurrentAdminUserId(),
            AiModelId = request.AiModelId,
            AllowFunctionCalls = request.AllowFunctionCalls,
            HumanInTheLoopLevel = request.HumanInTheLoopLevel,
            PluginToolPermission = request.PluginToolPermission,
            McpToolPermission = request.McpToolPermission,
            UseTemplateModelSettings = request.UseTemplateModelSettings
        };
    }

    private int GetCurrentAdminUserId()
    {
        try
        {
            return base.GetService<IAdminWorkContextProvider>()
                ?.GetAdminWorkContext()
                ?.AdminUserId ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}

public sealed record AgentExecutionAgentOption(int Id, string Name);
