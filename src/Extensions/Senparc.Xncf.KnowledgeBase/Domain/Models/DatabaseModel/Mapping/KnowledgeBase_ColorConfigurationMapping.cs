/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: KnowledgeBase_ColorConfigurationMapping.cs

    File description: KnowledgeBase_ColorConfigurationMapping related implementation

    

    

    Created by: Senparc - 20250105

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;

namespace Senparc.Xncf.KnowledgeBase.Models
{
    [XncfAutoConfigurationMapping]
    public class KnowledgeBase_ColorConfigurationMapping : ConfigurationMappingWithIdBase<Color, int>
    {
        public override void Configure(EntityTypeBuilder<Color> builder)
        {
            builder.Property(e => e.Red).IsRequired();
            builder.Property(e => e.Green).IsRequired();
            builder.Property(e => e.Blue).IsRequired();
        }
    }
}
