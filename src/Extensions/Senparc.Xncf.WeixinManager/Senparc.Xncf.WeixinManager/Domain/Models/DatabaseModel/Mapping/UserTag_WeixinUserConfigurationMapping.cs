/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: UserTag_WeixinUserConfigurationMapping.cs

    File description: UserTag_WeixinUserConfigurationMapping related implementation

    

    

    Created by: Senparc - 20250712

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Mapping
{
    public class UserTag_WeixinUserConfigurationMapping : ConfigurationMappingBase<UserTag_WeixinUser>
    {
        public override void Configure(EntityTypeBuilder<UserTag_WeixinUser> builder)
        {
            base.Configure(builder);

            builder.HasKey(uw => new { uw.UserTagId, uw.WeixinUserId });

            builder.HasOne(z => z.UserTag).WithMany(z => z.UserTags_WeixinUsers).HasForeignKey(z => z.UserTagId);
            builder.HasOne(z => z.WeixinUser).WithMany(z => z.UserTags_WeixinUsers).HasForeignKey(z => z.WeixinUserId);
        }
    }
}
