using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;

[Table(Register.DATABASE_PREFIX + nameof(WeixinClawMessageReceipt))]
[Serializable]
public class WeixinClawMessageReceipt : EntityBase<int>
{
    public int WeixinClawAccountId { get; private set; }

    [Required, MaxLength(200)]
    public string MessageId { get; private set; }

    public long Seq { get; private set; }

    public DateTime ReceivedAt { get; private set; }

    private WeixinClawMessageReceipt()
    {
    }

    public WeixinClawMessageReceipt(int accountId, string messageId, long seq)
    {
        WeixinClawAccountId = accountId;
        MessageId = messageId;
        Seq = seq;
        ReceivedAt = DateTime.UtcNow;
        SetUpdateTime();
    }
}
