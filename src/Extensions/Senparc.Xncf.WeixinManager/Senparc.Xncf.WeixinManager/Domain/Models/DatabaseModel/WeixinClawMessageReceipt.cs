/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawMessageReceipt.cs
    File description: WeixinClawMessageReceipt.cs implementation and project behavior.

    Created by: Senparc - 20260920

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

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

    /// <summary>
    /// Reserved for EF Core materialization.
    /// </summary>
    private WeixinClawMessageReceipt()
    {
    }

    /// <summary>
    /// Initializes a received-message receipt used for duplicate suppression.
    /// </summary>
    /// <param name="accountId">The WeixinClaw account identifier.</param>
    /// <param name="messageId">The provider message identifier.</param>
    /// <param name="seq">The provider sequence number.</param>
    public WeixinClawMessageReceipt(int accountId, string messageId, long seq)
    {
        WeixinClawAccountId = accountId;
        MessageId = messageId;
        Seq = seq;
        ReceivedAt = DateTime.UtcNow;
        SetUpdateTime();
    }
}
