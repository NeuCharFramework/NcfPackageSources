/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatGroupRunState.cs
    文件功能描述：ChatGroup 单次执行的运行时状态（从 ChatGroupService.RunChatGroupExecutionCoreAsync 抽取）
    
    创建标识：Senparc - 20260927
    创建描述：拆分 ChatGroupService，集中管理工作流事件循环中的可变状态

----------------------------------------------------------------*/

using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
/// ChatGroup 单次执行（RunChatGroupExecutionCoreAsync）期间的工作流可变状态。
/// </summary>
internal sealed class ChatGroupRunState
{
    public string ActiveResponseKey { get; set; }
    public string ActiveExecutorId { get; set; }
    public StringBuilder ActiveResponseText { get; } = new();
    public UsageDetails ActiveUsageDetails { get; set; }
    public DateTime? ActiveResponseStartedAt { get; set; }
    public int RoundIndex { get; set; }
    public bool ShouldExit { get; set; }
    public bool RoundLimitReached { get; set; }
    public HashSet<string> FinalizedResponseKeys { get; } = new(StringComparer.Ordinal);
    public HashSet<string> StreamedResponseKeys { get; } = new(StringComparer.Ordinal);
    public HashSet<string> ObservedWorkflowEventTypes { get; } = new(StringComparer.Ordinal);
    public List<PendingHumanRequest> WorkflowPendingRequests { get; } = new();
    public bool WorkflowFailed { get; set; }
    public string WorkflowFailureReason { get; set; } = string.Empty;
    public bool ToolInvocationFailed { get; set; }
    public string ToolFailureExecutorId { get; set; } = string.Empty;
    public Dictionary<string, string> PendingToolNamesByCallId { get; } = new(StringComparer.Ordinal);
}
