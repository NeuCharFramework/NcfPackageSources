/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawConfigurationMapping.cs
    File description: WeixinClawConfigurationMapping.cs implementation and project behavior.

    Created by: Senparc - 20260920

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 enhance WeixinClaw message handling and UI features

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Mapping;

public class WeixinClawConfigurationMapping : ConfigurationMappingWithIdBase<WeixinClawAccount, int>
{
    public override void Configure(EntityTypeBuilder<WeixinClawAccount> builder)
    {
        base.Configure(builder);
        builder.HasIndex(z => z.IlinkBotId).IsUnique();
    }
}

public class WeixinClawMessageReceiptConfigurationMapping : ConfigurationMappingWithIdBase<WeixinClawMessageReceipt, int>
{
    public override void Configure(EntityTypeBuilder<WeixinClawMessageReceipt> builder)
    {
        base.Configure(builder);
        builder.HasIndex(z => new { z.WeixinClawAccountId, z.MessageId, z.Seq }).IsUnique();
        builder.HasOne<WeixinClawAccount>()
            .WithMany()
            .HasForeignKey(z => z.WeixinClawAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WeixinClawMessageRecordConfigurationMapping
    : ConfigurationMappingWithIdBase<WeixinClawMessageRecord, int>
{
    public override void Configure(EntityTypeBuilder<WeixinClawMessageRecord> builder)
    {
        base.Configure(builder);
        builder.HasIndex(z => new { z.WeixinClawAccountId, z.CreatedAt });
        builder.HasIndex(z => new { z.WeixinClawAccountId, z.Direction, z.MessageId });
    }
}
