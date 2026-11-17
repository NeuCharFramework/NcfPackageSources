/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinUserConfigurationMapping.cs
    File description: WeixinUserConfigurationMapping related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Mapping
{
    public class WeixinUserConfigurationMapping : ConfigurationMappingWithIdBase<WeixinUser, int>
    {
        public override void Configure(EntityTypeBuilder<WeixinUser> builder)
        {
            base.Configure(builder);

            builder.HasOne(z => z.MpAccount)
                .WithMany(z => z.WeixinUsers)
                .HasForeignKey(z => z.MpAccountId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName($"FK__{nameof(WeixinUser)}__{nameof(WeixinUser.MpAccountId)}");
        }
    }
}
