/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatGroupRunState.cs
    File description: Related implementation.

    Created by: Senparc - 20260927
    Creation description: Related implementation.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

----------------------------------------------------------------*/

using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
///Legacy comment translated to English.
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
