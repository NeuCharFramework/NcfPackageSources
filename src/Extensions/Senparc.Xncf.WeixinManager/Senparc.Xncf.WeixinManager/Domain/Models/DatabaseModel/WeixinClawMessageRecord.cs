/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawMessageRecord.cs
    File description: WeixinClawMessageRecord.cs implementation and project behavior.

    Created by: Senparc - 20260927

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 enhance WeixinClaw media handling and UI improvements

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;

[Table(Register.DATABASE_PREFIX + nameof(WeixinClawMessageRecord))]
[Serializable]
    /// Stores one inbound or outbound Weixin Claw message and its delivery state.
    public sealed class WeixinClawMessageRecord : EntityBase<int>
{
    public int WeixinClawAccountId { get; private set; }

    [Required, MaxLength(20)]
    public string Direction { get; private set; }

    [MaxLength(200)]
    public string MessageId { get; private set; }

    public long? Seq { get; private set; }

    [MaxLength(300)]
    public string FromUserId { get; private set; }

    [MaxLength(300)]
    public string ToUserId { get; private set; }

    [MaxLength(300)]
    public string ClientId { get; private set; }

    [MaxLength(4096)]
    public string ContextTokenProtected { get; private set; }

    [MaxLength(300)]
    public string RunId { get; private set; }

    public int MessageType { get; private set; }
    public int MessageState { get; private set; }

    [Required, MaxLength(30)]
    public string Status { get; private set; }

    [Required]
    public DateTime CreatedAt { get; private set; }

    [MaxLength(2000)]
    public string Error { get; private set; }

    [Required]
    public string Text { get; private set; }

    /// <summary>
    /// Reserved for EF Core materialization.
    /// </summary>
    private WeixinClawMessageRecord()
    {
    }

    /// <summary>
    /// Initializes a message record with protocol identity, conversation context, and status.
    /// </summary>
    /// <param name="accountId">The Claw account identifier.</param>
    /// <param name="direction">The inbound/outbound direction.</param>
    /// <param name="messageId">The remote message identifier.</param>
    /// <param name="seq">The remote sequence number.</param>
    /// <param name="fromUserId">The sender identifier.</param>
    /// <param name="toUserId">The recipient identifier.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="contextTokenProtected">The protected conversation context token.</param>
    /// <param name="runId">The associated workflow run identifier.</param>
    /// <param name="messageType">The protocol message type.</param>
    /// <param name="messageState">The protocol message state.</param>
    /// <param name="status">The initial persistence/delivery status.</param>
    /// <param name="text">The message text.</param>
    /// <param name="createdAt">Optional creation timestamp.</param>
    public WeixinClawMessageRecord(
        int accountId,
        string direction,
        string messageId,
        long? seq,
        string fromUserId,
        string toUserId,
        string clientId,
        string contextTokenProtected,
        string runId,
        int messageType,
        int messageState,
        string status,
        string text,
        DateTime? createdAt = null)
    {
        WeixinClawAccountId = accountId;
        Direction = direction?.Trim() ?? string.Empty;
        MessageId = messageId;
        Seq = seq;
        FromUserId = fromUserId;
        ToUserId = toUserId;
        ClientId = clientId;
        ContextTokenProtected = contextTokenProtected;
        RunId = runId;
        MessageType = messageType;
        MessageState = messageState;
        Status = status?.Trim() ?? string.Empty;
        Text = text ?? string.Empty;
        CreatedAt = createdAt ?? DateTime.UtcNow;
        SetUpdateTime();
    }

    /// <summary>
    /// Marks the record as sent and stores the remote message identifier.
    /// </summary>
    /// <param name="messageId">The remote message identifier.</param>
    public void MarkSent(string messageId)
    {
        MessageId = messageId;
        Status = "sent";
        Error = null;
        SetUpdateTime();
    }

    /// <summary>
    /// Marks the record as failed and stores a bounded diagnostic message.
    /// </summary>
    /// <param name="error">The failure message.</param>
    public void MarkFailed(string error)
    {
        Status = "failed";
        Error = error?.Length > 2000 ? error[..2000] : error;
        SetUpdateTime();
    }

    /// <summary>
    /// Replaces the stored message text.
    /// </summary>
    /// <param name="text">The replacement message text.</param>
    public void SetText(string text)
    {
        Text = text ?? string.Empty;
        SetUpdateTime();
    }
}

public static class WeixinClawMessageDirection
{
    public const string Inbound = "inbound";
    public const string Outbound = "outbound";
}
