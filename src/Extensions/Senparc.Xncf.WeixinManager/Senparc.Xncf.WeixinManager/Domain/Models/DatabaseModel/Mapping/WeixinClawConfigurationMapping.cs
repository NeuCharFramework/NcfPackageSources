/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawConfigurationMapping.cs
    文件功能描述：WeixinClawConfigurationMapping.cs implementation and project behavior.


    创建标识：Senparc - 20260920

    修改标识：Senparc - 20261005
    修改描述：v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

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
