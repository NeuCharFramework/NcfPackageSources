/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：UserTag_WeixinUserConfigurationMapping.cs
    文件功能描述：UserTag_WeixinUserConfigurationMapping 相关实现
    
    
    创建标识：Senparc - 20250712
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
