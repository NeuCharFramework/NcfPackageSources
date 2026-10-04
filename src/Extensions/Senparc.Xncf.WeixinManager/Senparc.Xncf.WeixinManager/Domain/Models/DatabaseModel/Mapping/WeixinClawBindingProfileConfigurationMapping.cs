/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawBindingProfileConfigurationMapping.cs
    文件功能描述：WeixinClawBindingProfileConfigurationMapping.cs implementation and project behavior.


    创建标识：Senparc - 20260928

    修改标识：Senparc - 20261005
    修改描述：v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

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
