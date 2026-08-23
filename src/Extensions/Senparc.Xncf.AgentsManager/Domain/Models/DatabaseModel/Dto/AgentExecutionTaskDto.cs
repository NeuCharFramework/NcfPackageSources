/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AgentExecutionTaskDto.cs
    文件功能描述：独立 Agent 执行任务 DTO

    创建标识：Senparc - 20260822

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 统一独立 Agent 执行记录和管理接口

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 新增独立 Agent 执行任务持久化、管理页和 SSE 过程回放


----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Domain.Services;
using System;
using System.Collections.Generic;

namespace Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;

public class AgentExecutionTaskDto : DtoBase<int>
{
    public int AgentTemplateId { get; set; }
    public string AgentTemplateName { get; set; }
    public string Name { get; set; }
    public string Source { get; set; }
    public string CorrelationId { get; set; }
    public string ExternalReference { get; set; }
    public int? WorkflowId { get; set; }
    public int AdminUserId { get; set; }
    public int? AiModelId { get; set; }
    public string ModelDescription { get; set; }
    public string PromptCommand { get; set; }
    public string Output { get; set; }
    public string ErrorMessage { get; set; }
    public string EventsJson { get; set; }
    public AgentExecutionTask_Status Status { get; set; }
    public bool AllowFunctionCalls { get; set; }
    public HumanInTheLoopLevel HumanInTheLoopLevel { get; set; }
    public ToolPermissionMode PluginToolPermission { get; set; }
    public ToolPermissionMode McpToolPermission { get; set; }
    public bool IsPersonality { get; set; }
    public bool IsArchived { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int TotalPromptTokens { get; set; }
    public int TotalCompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public int ToolCallCount { get; set; }
    public int ResponseCount { get; set; }
    public int TotalResponseMilliseconds { get; set; }
    public int MaxResponseMilliseconds { get; set; }
    public double AverageResponseMilliseconds =>
        ResponseCount <= 0
            ? 0
            : (double)TotalResponseMilliseconds / ResponseCount;

    public List<AgentExecutionEventDto> Events { get; set; } = new();

    public AgentExecutionTaskDto()
    {
    }

    public AgentExecutionTaskDto(AgentExecutionTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        Id = task.Id;
        AgentTemplateId = task.AgentTemplateId;
        AgentTemplateName = task.AgentTemplateName;
        Name = task.Name;
        Source = task.Source;
        CorrelationId = task.CorrelationId;
        ExternalReference = task.ExternalReference;
        WorkflowId = task.WorkflowId;
        AdminUserId = task.AdminUserId;
        AiModelId = task.AiModelId;
        ModelDescription = task.ModelDescription;
        PromptCommand = task.PromptCommand;
        Output = task.Output;
        ErrorMessage = task.ErrorMessage;
        EventsJson = task.EventsJson;
        Status = task.Status;
        AllowFunctionCalls = task.AllowFunctionCalls;
        HumanInTheLoopLevel = task.HumanInTheLoopLevel;
        PluginToolPermission = task.PluginToolPermission;
        McpToolPermission = task.McpToolPermission;
        IsPersonality = task.IsPersonality;
        IsArchived = task.IsArchived;
        StartTime = task.StartTime;
        EndTime = task.EndTime;
        TotalPromptTokens = task.TotalPromptTokens;
        TotalCompletionTokens = task.TotalCompletionTokens;
        TotalTokens = task.TotalTokens;
        ToolCallCount = task.ToolCallCount;
        ResponseCount = task.ResponseCount;
        TotalResponseMilliseconds = task.TotalResponseMilliseconds;
        MaxResponseMilliseconds = task.MaxResponseMilliseconds;
    }
}

public sealed class AgentExecutionEventDto
{
    public int Sequence { get; set; }
    public string EventType { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
    public string ToolName { get; set; }
    public string ToolArguments { get; set; }
    public string ToolResult { get; set; }
    public string ErrorMessage { get; set; }
    public string ResponseId { get; set; }
    public string Text { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public int ResponseMilliseconds { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
