/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenMapic_ColorConfigurationMapping.cs
    File description: SenMapic_ColorConfigurationMapping related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.4.13.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;

namespace Senparc.Xncf.SenMapic.Models
{
    [XncfAutoConfigurationMapping]
    public class SenMapic_ColorConfigurationMapping : ConfigurationMappingWithIdBase<Color, int>
    {
        /// <inheritdoc />
        public override void Configure(EntityTypeBuilder<Color> builder)
        {
            builder.Property(e => e.Red).IsRequired();
            builder.Property(e => e.Green).IsRequired();
            builder.Property(e => e.Blue).IsRequired();
        }
    }
}
