/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AgentExecutionTask.cs
    文件功能描述：独立 Agent 执行任务持久化模型

    创建标识：Senparc - 20260822

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 统一 Workflow、管理页和外部调用的独立 Agent 执行记录

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 新增独立 Agent 执行任务持久化、管理页和 SSE 过程回放


----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;

[Table(Register.DATABASE_PREFIX + nameof(AgentExecutionTask))]
[Serializable]
public class AgentExecutionTask : EntityBase<int>
{
    [Required]
    public int AgentTemplateId { get; private set; }

    public AgentTemplate AgentTemplate { get; private set; }

    [Required, MaxLength(150)]
    public string AgentTemplateName { get; private set; }

    [Required, MaxLength(150)]
    public string Name { get; private set; }

    [Required, MaxLength(40)]
    public string Source { get; private set; }

    [MaxLength(200)]
    public string CorrelationId { get; private set; }

    [MaxLength(200)]
    public string ExternalReference { get; private set; }

    public int? WorkflowId { get; private set; }

    public int AdminUserId { get; private set; }

    public int? AiModelId { get; private set; }

    [MaxLength(1000)]
    public string ModelDescription { get; private set; }

    [Required]
    public string PromptCommand { get; private set; }

    public string Output { get; private set; }

    public string ErrorMessage { get; private set; }

    /// <summary>按执行顺序保存的独立 Agent 过程事件。</summary>
    public string EventsJson { get; private set; } = "[]";

    public AgentExecutionTask_Status Status { get; private set; }

    public bool AllowFunctionCalls { get; private set; }

    public HumanInTheLoopLevel HumanInTheLoopLevel { get; private set; }

    public ToolPermissionMode PluginToolPermission { get; private set; }

    public ToolPermissionMode McpToolPermission { get; private set; }

    public bool IsPersonality { get; private set; }

    public bool IsArchived { get; private set; }

    [Required]
    public DateTime StartTime { get; private set; }

    public DateTime? EndTime { get; private set; }

    public int TotalPromptTokens { get; private set; }

    public int TotalCompletionTokens { get; private set; }

    public int TotalTokens { get; private set; }

    public int ToolCallCount { get; private set; }

    public int ResponseCount { get; private set; }

    public int TotalResponseMilliseconds { get; private set; }

    public int MaxResponseMilliseconds { get; private set; }

    private AgentExecutionTask()
    {
    }

    public AgentExecutionTask(AgentExecutionTaskDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        AgentTemplateId = dto.AgentTemplateId;
        AgentTemplateName = dto.AgentTemplateName;
        Name = dto.Name;
        Source = dto.Source;
        CorrelationId = dto.CorrelationId;
        ExternalReference = dto.ExternalReference;
        WorkflowId = dto.WorkflowId;
        AdminUserId = dto.AdminUserId;
        AiModelId = dto.AiModelId;
        ModelDescription = dto.ModelDescription;
        PromptCommand = dto.PromptCommand;
        Output = dto.Output;
        ErrorMessage = dto.ErrorMessage;
        EventsJson = string.IsNullOrWhiteSpace(dto.EventsJson) ? "[]" : dto.EventsJson;
        Status = dto.Status;
        AllowFunctionCalls = dto.AllowFunctionCalls;
        HumanInTheLoopLevel = dto.HumanInTheLoopLevel;
        PluginToolPermission = dto.PluginToolPermission;
        McpToolPermission = dto.McpToolPermission;
        IsPersonality = dto.IsPersonality;
        IsArchived = dto.IsArchived;
        StartTime = dto.StartTime == default ? DateTime.Now : dto.StartTime;
        EndTime = dto.EndTime;
        TotalPromptTokens = dto.TotalPromptTokens;
        TotalCompletionTokens = dto.TotalCompletionTokens;
        TotalTokens = dto.TotalTokens;
        ToolCallCount = dto.ToolCallCount;
        ResponseCount = dto.ResponseCount;
        TotalResponseMilliseconds = dto.TotalResponseMilliseconds;
        MaxResponseMilliseconds = dto.MaxResponseMilliseconds;
    }

    public void ChangeStatus(AgentExecutionTask_Status status)
    {
        Status = status;
        if (status is AgentExecutionTask_Status.Finished
            or AgentExecutionTask_Status.Cancelled
            or AgentExecutionTask_Status.Failed)
        {
            EndTime = DateTime.Now;
        }
    }

    public void SetOutput(string output)
    {
        Output = output;
    }

    public void SetError(string errorMessage)
    {
        ErrorMessage = errorMessage;
    }

    public void SetEvents(string eventsJson)
    {
        EventsJson = string.IsNullOrWhiteSpace(eventsJson) ? "[]" : eventsJson;
    }

    public void SetModel(int? aiModelId, string modelDescription)
    {
        AiModelId = aiModelId;
        ModelDescription = modelDescription;
    }

    public void AddUsage(int promptTokens, int completionTokens, int totalTokens, int responseMilliseconds)
    {
        ResponseCount++;
        TotalPromptTokens += Math.Max(0, promptTokens);
        TotalCompletionTokens += Math.Max(0, completionTokens);
        TotalTokens += Math.Max(0, totalTokens > 0 ? totalTokens : promptTokens + completionTokens);
        TotalResponseMilliseconds += Math.Max(0, responseMilliseconds);
        MaxResponseMilliseconds = Math.Max(MaxResponseMilliseconds, responseMilliseconds);
    }

    public void AddToolCall()
    {
        ToolCallCount++;
    }

    public void SetArchived(bool isArchived)
    {
        IsArchived = isArchived;
    }
}

public enum AgentExecutionTask_Status
{
    Waiting = 0,
    Running = 1,
    Paused = 2,
    Finished = 3,
    Cancelled = 4,
    Failed = 5
}
