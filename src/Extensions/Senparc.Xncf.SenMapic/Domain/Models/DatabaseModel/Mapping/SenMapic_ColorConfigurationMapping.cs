/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: SenMapic_ColorConfigurationMapping.cs

    File description: SenMapic_ColorConfigurationMapping related implementation

    

    

    Created by: Senparc - 20260704

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;

namespace Senparc.Xncf.SenMapic.Models
{
    [XncfAutoConfigurationMapping]
    public class SenMapic_ColorConfigurationMapping : ConfigurationMappingWithIdBase<Color, int>
    {
        public override void Configure(EntityTypeBuilder<Color> builder)
        {
            builder.Property(e => e.Red).IsRequired();
            builder.Property(e => e.Green).IsRequired();
            builder.Property(e => e.Blue).IsRequired();
        }
    }
}
