/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XscfModuleAccountConfigurationMapping.cs
    文件功能描述：XscfModuleAccountConfigurationMapping 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfModuleManager.DataBaseModel
{
    public class XncfModuleAccountConfigurationMapping : ConfigurationMappingWithIdBase<XncfModule, int>
    {
        public override void Configure(EntityTypeBuilder<XncfModule> builder)
        {
            base.Configure(builder);

            builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
            builder.Property(e => e.Uid).HasMaxLength(100).IsRequired();
            builder.Property(e => e.MenuName).HasMaxLength(100);
            builder.Property(e => e.Version).HasMaxLength(100).IsRequired();
            builder.Property(e => e.AllowRemove).IsRequired();
            builder.Property(e => e.MenuId).HasMaxLength(100);
            builder.Property(e => e.State).IsRequired();
            builder.Property(e => e.Icon).HasMaxLength(100);

            var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.Current;
            if (currentDatabaseConfiguration.MultipleDatabaseType == MultipleDatabaseType.MySql ||
                currentDatabaseConfiguration.MultipleDatabaseType == MultipleDatabaseType.PostgreSQL|| currentDatabaseConfiguration.MultipleDatabaseType == MultipleDatabaseType.Dm)
            {
                builder.Property(e => e.UpdateLog).HasColumnType("text").IsRequired();
            }
            else
            {
                builder.Property(e => e.UpdateLog).HasColumnType("ntext").IsRequired();
            }
        }
    }
}
