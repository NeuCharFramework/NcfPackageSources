using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;

namespace Senparc.Xncf.SenMapic.Models
{
    [XncfAutoConfigurationMapping]
    public class SenMapic_TaskConfigurationMapping : ConfigurationMappingWithIdBase<SenMapicTask, int>
    {
        public override void Configure(EntityTypeBuilder<SenMapicTask> builder)
        {
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
            builder.Property(e => e.StartUrl).IsRequired().HasMaxLength(1000);
            builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
        }
    }
} 