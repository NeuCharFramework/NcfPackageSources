/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawBindingProfileConfigurationMapping.cs
    File description: WeixinClawBindingProfileConfigurationMapping.cs implementation and project behavior.

    Created by: Senparc - 20260928

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 add WeixinClawBindingProfile management and UI enhancements

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

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
