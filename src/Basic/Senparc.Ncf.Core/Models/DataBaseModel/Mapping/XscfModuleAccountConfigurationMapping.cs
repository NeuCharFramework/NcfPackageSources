using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    public class XscfModuleAccountConfigurationMapping : ConfigurationMappingWithIdBase<XscfModule, int>
    {
        public override void Configure(EntityTypeBuilder<XscfModule> builder)
        {
            base.Configure(builder);

            builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
            builder.Property(e => e.Uid).HasMaxLength(100).IsRequired();
            builder.Property(e => e.MenuName).HasMaxLength(100);
            builder.Property(e => e.Version).HasMaxLength(100).IsRequired();
            builder.Property(e => e.UpdateLog).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.AllowRemove).IsRequired();
            builder.Property(e => e.MenuId).HasMaxLength(100);
            builder.Property(e => e.State).IsRequired();
        }
    }
}
