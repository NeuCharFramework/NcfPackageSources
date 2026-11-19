/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FileManager_ColorConfigurationMapping.cs
    File description: FileManager_ColorConfigurationMapping related implementation

    Created by: Senparc - 20250105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.8.5 Localized file management and upload surfaces

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;

namespace Senparc.Xncf.FileManager.Models
{
    [XncfAutoConfigurationMapping]
    public class FileManager_ColorConfigurationMapping : ConfigurationMappingWithIdBase<Color, int>
    {
        public override void Configure(EntityTypeBuilder<Color> builder)
        {
            builder.Property(e => e.Red).IsRequired();
            builder.Property(e => e.Green).IsRequired();
            builder.Property(e => e.Blue).IsRequired();
        }
    }
}
