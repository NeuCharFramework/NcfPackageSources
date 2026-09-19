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
        builder.HasIndex(z => z.IlinkBotId);
    }
}

public class WeixinClawMessageReceiptConfigurationMapping : ConfigurationMappingWithIdBase<WeixinClawMessageReceipt, int>
{
    public override void Configure(EntityTypeBuilder<WeixinClawMessageReceipt> builder)
    {
        base.Configure(builder);
        builder.HasIndex(z => new { z.WeixinClawAccountId, z.MessageId, z.Seq }).IsUnique();
    }
}
