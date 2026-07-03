/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WeixinUserConfigurationMapping.cs
    文件功能描述：WeixinUserConfigurationMapping 相关实现
    
    
    创建标识：Senparc - 20250712
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
