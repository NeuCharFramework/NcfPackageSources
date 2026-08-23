/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AgentExecutionRequest.cs
    文件功能描述：独立 Agent 执行接口请求与响应 DTO

    创建标识：Senparc - 20260822

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 统一独立 Agent 管理页和外部调用契约

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 新增独立 Agent 执行任务持久化、管理页和 SSE 过程回放


----------------------------------------------------------------*/

using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Domain.Services;
using System.Collections.Generic;

namespace Senparc.Xncf.AgentsManager.Application.DTOs;

public sealed class AgentExecutionStartRequest
{
    public int AgentTemplateId { get; set; }
    public string Name { get; set; }
    public string Input { get; set; }
    public string Source { get; set; }
    public string CorrelationId { get; set; }
    public string ExternalReference { get; set; }
    public int? WorkflowId { get; set; }
    public int? AiModelId { get; set; }
    public bool AllowFunctionCalls { get; set; }
    public HumanInTheLoopLevel HumanInTheLoopLevel { get; set; } = HumanInTheLoopLevel.Automatic;
    public ToolPermissionMode PluginToolPermission { get; set; } = ToolPermissionMode.Inherit;
    public ToolPermissionMode McpToolPermission { get; set; } = ToolPermissionMode.Inherit;
    public bool UseTemplateModelSettings { get; set; } = true;
}

public sealed class AgentExecutionStartResponse
{
    public AgentExecutionTaskDto Task { get; set; }
}

public sealed class AgentExecutionRunResponse
{
    public AgentExecutionTaskDto Task { get; set; }
    public AgentExecutionResult Result { get; set; }
}

public sealed class AgentExecutionListResponse
{
    public IReadOnlyList<AgentExecutionTaskDto> Tasks { get; set; } = new List<AgentExecutionTaskDto>();
}
