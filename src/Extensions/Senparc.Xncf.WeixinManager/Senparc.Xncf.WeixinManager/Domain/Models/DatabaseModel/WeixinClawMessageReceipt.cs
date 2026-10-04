/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawMessageReceipt.cs
    文件功能描述：WeixinClawMessageReceipt.cs implementation and project behavior.


    创建标识：Senparc - 20260920

    修改标识：Senparc - 20261005
    修改描述：v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

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
