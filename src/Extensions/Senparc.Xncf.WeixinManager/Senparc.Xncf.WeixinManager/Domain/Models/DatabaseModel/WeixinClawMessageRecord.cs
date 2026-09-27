using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;

[Table(Register.DATABASE_PREFIX + nameof(WeixinClawMessageRecord))]
[Serializable]
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

    private WeixinClawMessageRecord()
    {
    }

    public WeixinClawMessageRecord(
        int accountId,
        string direction,
        string messageId,
        long? seq,
        string fromUserId,
        string toUserId,
        string clientId,
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
        MessageType = messageType;
        MessageState = messageState;
        Status = status?.Trim() ?? string.Empty;
        Text = text ?? string.Empty;
        CreatedAt = createdAt ?? DateTime.UtcNow;
        SetUpdateTime();
    }

    public void MarkSent(string messageId)
    {
        MessageId = messageId;
        Status = "sent";
        Error = null;
        SetUpdateTime();
    }

    public void MarkFailed(string error)
    {
        Status = "failed";
        Error = error?.Length > 2000 ? error[..2000] : error;
        SetUpdateTime();
    }
}

public static class WeixinClawMessageDirection
{
    public const string Inbound = "inbound";
    public const string Outbound = "outbound";
}
