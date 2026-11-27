/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawAdminBinding.cs
    File description: WeixinClawAdminBinding.cs implementation and project behavior.

    Created by: Senparc - 20260926

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 enhance WeixinClaw integration with new message handling and UI improvements

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text.Json;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel;

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(WeixinClawAdminBinding))]
[Serializable]
public sealed class WeixinClawAdminBinding : EntityBase<int>
{
    [Required]
    public int AccountId { get; private set; }

    [Required, MaxLength(300)]
    public string FromUserId { get; private set; }

    [MaxLength(300)]
    public string GroupId { get; private set; }

    [Required]
    public int AdminUserId { get; private set; }

    public int? AdminChatSessionId { get; private set; }
    public int? LastTrajectoryId { get; private set; }
    public int LastTrajectorySequence { get; private set; }
    public int AiModelId { get; private set; }
    public int? WorkflowId { get; private set; }
    public int Mode { get; private set; }
    public bool Enabled { get; private set; }
    public bool EnableNeuBell { get; private set; }
    public bool EnableWorkflow { get; private set; }

    [MaxLength(1500)]
    public string ContextToken { get; private set; }

    [MaxLength(2000)]
    public string NeuBellPushStateJson { get; private set; }

    [MaxLength(300)]
    public string PendingApprovalRequestId { get; private set; }

    [MaxLength(300)]
    public string PendingApprovalToolCallId { get; private set; }

    [MaxLength(300)]
    public string PendingApprovalToolName { get; private set; }

    public string PendingApprovalArgumentsJson { get; private set; }

    public DateTime? LastMessageAt { get; private set; }

    private WeixinClawAdminBinding()
    {
    }

    /// <summary>
    /// Initializes an enabled sender/group binding with its NeuBell and workflow flags.
    /// </summary>
    /// <param name="accountId">The Weixin Claw account identifier.</param>
    /// <param name="fromUserId">The sender identifier.</param>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="adminUserId">The owning administrator identifier.</param>
    /// <param name="enableNeuBell">Whether NeuBell routing is enabled.</param>
    /// <param name="enableWorkflow">Whether workflow routing is enabled.</param>
    public WeixinClawAdminBinding(
        int accountId,
        string fromUserId,
        string groupId,
        int adminUserId,
        bool enableNeuBell,
        bool enableWorkflow)
    {
        AccountId = accountId;
        FromUserId = fromUserId?.Trim() ?? string.Empty;
        GroupId = groupId?.Trim() ?? string.Empty;
        AdminUserId = adminUserId;
        Mode = 0;
        Enabled = true;
        EnableNeuBell = enableNeuBell;
        EnableWorkflow = enableWorkflow;
        AiModelId = 0;
    }

    /// <summary>
    /// Associates the binding with a chat session, clearing it for non-positive input.
    /// </summary>
    /// <param name="sessionId">The chat session identifier.</param>
    public void SetChatSession(int sessionId)
    {
        AdminChatSessionId = sessionId > 0 ? sessionId : null;
        SetUpdateTime();
    }

    /// <summary>
    /// Stores the latest conversation context, trajectory position, and message time.
    /// </summary>
    /// <param name="contextToken">The provider conversation token.</param>
    /// <param name="trajectoryId">The latest trajectory identifier.</param>
    /// <param name="trajectorySequence">The latest trajectory sequence.</param>
    /// <param name="messageAt">The latest message time.</param>
    public void SetConversationState(
        string contextToken,
        int? trajectoryId,
        int trajectorySequence,
        DateTime messageAt)
    {
        ContextToken = contextToken;
        LastTrajectoryId = trajectoryId;
        LastTrajectorySequence = trajectorySequence;
        LastMessageAt = messageAt;
        SetUpdateTime();
    }

    /// <summary>
    /// Stores only the provider context token and latest message time.
    /// </summary>
    /// <param name="contextToken">The provider conversation token.</param>
    /// <param name="messageAt">The message time.</param>
    public void SetContextToken(string contextToken, DateTime messageAt)
    {
        ContextToken = string.IsNullOrWhiteSpace(contextToken) ? null : contextToken;
        LastMessageAt = messageAt;
        SetUpdateTime();
    }

    /// <summary>
    /// Determines whether the same provider fingerprint has already been pushed.
    /// Invalid identifiers return <see langword="false"/>.
    /// </summary>
    /// <param name="providerId">The NeuBell provider identifier.</param>
    /// <param name="fingerprint">The provider snapshot fingerprint.</param>
    /// <returns><see langword="true"/> when the fingerprint was already recorded.</returns>
    public bool HasNeuBellPush(string providerId, string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(fingerprint))
        {
            return false;
        }

        var state = ReadNeuBellPushState();
        return state.TryGetValue(providerId, out var previous)
            && string.Equals(previous, fingerprint, StringComparison.Ordinal);
    }

    /// <summary>
    /// Records the latest pushed fingerprint for a provider.
    /// </summary>
    /// <param name="providerId">The NeuBell provider identifier.</param>
    /// <param name="fingerprint">The provider snapshot fingerprint.</param>
    public void MarkNeuBellPushed(string providerId, string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(fingerprint))
        {
            return;
        }

        var state = ReadNeuBellPushState();
        state[providerId] = fingerprint;
        NeuBellPushStateJson = JsonSerializer.Serialize(state);
        SetUpdateTime();
    }

    /// <summary>
    /// Reads the persisted provider-fingerprint map, treating blank or malformed
    /// JSON as an empty state.
    /// </summary>
    /// <returns>A case-insensitive provider-fingerprint map.</returns>
    private Dictionary<string, string> ReadNeuBellPushState()
    {
        if (string.IsNullOrWhiteSpace(NeuBellPushStateJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var state = JsonSerializer.Deserialize<Dictionary<string, string>>(NeuBellPushStateJson);
            return state == null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(state, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Stores the tool-call approval request currently awaiting administrator input.
    /// </summary>
    /// <param name="requestId">The approval request identifier.</param>
    /// <param name="toolCallId">The tool-call identifier.</param>
    /// <param name="toolName">The tool name.</param>
    /// <param name="argumentsJson">The serialized tool arguments.</param>
    public void SetPendingApproval(
        string requestId,
        string toolCallId,
        string toolName,
        string argumentsJson)
    {
        PendingApprovalRequestId = requestId;
        PendingApprovalToolCallId = toolCallId;
        PendingApprovalToolName = toolName;
        PendingApprovalArgumentsJson = argumentsJson;
        SetUpdateTime();
    }

    /// <summary>
    /// Clears all pending approval metadata from the binding.
    /// </summary>
    public void ClearPendingApproval()
    {
        PendingApprovalRequestId = null;
        PendingApprovalToolCallId = null;
        PendingApprovalToolName = null;
        PendingApprovalArgumentsJson = null;
        SetUpdateTime();
    }

    /// <summary>
    /// Updates AI model, workflow, mode, and feature-routing flags.
    /// </summary>
    /// <param name="mode">The routing mode.</param>
    /// <param name="aiModelId">The selected AI model identifier.</param>
    /// <param name="workflowId">The optional workflow identifier.</param>
    /// <param name="enableNeuBell">Whether NeuBell is enabled.</param>
    /// <param name="enableWorkflow">Whether workflow execution is enabled.</param>
    public void SetRouting(
        int mode,
        int aiModelId,
        int? workflowId,
        bool enableNeuBell,
        bool enableWorkflow)
    {
        Mode = mode;
        AiModelId = aiModelId;
        WorkflowId = workflowId;
        EnableNeuBell = enableNeuBell;
        EnableWorkflow = enableWorkflow;
        SetUpdateTime();
    }

    /// <summary>
    /// Disables the binding without deleting its historical conversation state.
    /// </summary>
    public void Disable()
    {
        Enabled = false;
        SetUpdateTime();
    }
}
