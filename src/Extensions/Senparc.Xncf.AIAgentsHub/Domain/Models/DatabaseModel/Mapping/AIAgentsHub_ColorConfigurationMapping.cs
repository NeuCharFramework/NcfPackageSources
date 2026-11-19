/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIAgentsHub_ColorConfigurationMapping.cs
    File description: AIAgentsHub_ColorConfigurationMapping related implementation

    Created by: Senparc - 20240311

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.13.16 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;

namespace Senparc.Xncf.AIAgentsHub.Models
{
    [XncfAutoConfigurationMapping]
    public class AIAgentsHub_ColorConfigurationMapping : ConfigurationMappingWithIdBase<Color, int>
    {
        public override void Configure(EntityTypeBuilder<Color> builder)
        {
            builder.Property(e => e.Red).IsRequired();
            builder.Property(e => e.Green).IsRequired();
            builder.Property(e => e.Blue).IsRequired();
        }
    }
}
