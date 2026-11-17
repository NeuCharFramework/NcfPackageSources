/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: Admin_KnowledgeBaseItemConfigurationMapping.cs
    File description: Admin_KnowledgeBaseItemConfigurationMapping related implementation
    
    
    Created by: Senparc - 20260704
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.5.0-preview6 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel;

namespace Senparc.Xncf.KnowledgeBase.Models
{
    [XncfAutoConfigurationMapping]
    public class Admin_KnowledgeBaseItemConfigurationMapping : ConfigurationMappingWithIdBase<KnowledgeBaseItem, int>
    {
        public override void Configure(EntityTypeBuilder<KnowledgeBaseItem> builder)
        {
            base.Configure(builder);
            builder.HasIndex(z => new { z.KnowledgeBasesId, z.NcfFileId, z.ChunkIndex });
        }
    }
}
