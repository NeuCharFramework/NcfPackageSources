using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
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
                currentDatabaseConfiguration.MultipleDatabaseType == MultipleDatabaseType.PostgreSQL)
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
