/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: UserTagConfigurationMapping.cs
    File description: UserTagConfigurationMapping related implementation

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
    public class UserTagConfigurationMapping : ConfigurationMappingWithIdBase<UserTag, int>
    {
        public override void Configure(EntityTypeBuilder<UserTag> builder)
        {
            base.Configure(builder);

            builder.HasOne(z => z.MpAccount)
               .WithMany(z => z.UserTags)
               .HasForeignKey(z => z.MpAccountId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName($"FK__{nameof(UserTag)}__{nameof(UserTag.MpAccountId)}");
        }
    }
}
