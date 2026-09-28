using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Mapping;

public sealed class WeixinClawBindingProfileConfigurationMapping
    : ConfigurationMappingWithIdBase<WeixinClawBindingProfile, int>
{
    public override void Configure(EntityTypeBuilder<WeixinClawBindingProfile> builder)
    {
        base.Configure(builder);
        builder.HasIndex(item => new
        {
            item.WeixinClawAccountId,
            item.BindingCodeHash
        }).IsUnique();
        builder.HasIndex(item => new
        {
            item.WeixinClawAccountId,
            item.Enabled
        });
    }
}
