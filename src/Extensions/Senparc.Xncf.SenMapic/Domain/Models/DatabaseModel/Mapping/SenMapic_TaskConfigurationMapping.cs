/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: SenMapic_TaskConfigurationMapping.cs

    File description: SenMapic_TaskConfigurationMapping related implementation

    

    

    Created by: Senparc - 20250114

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

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