/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatGroupConfigurationMapping.cs
    File description: ChatGroupConfigurationMapping related implementation

    Created by: Senparc - 20240616

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;

namespace Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Mapping
{
    [XncfAutoConfigurationMapping]
    public class ChatGroupConfigurationMapping : ConfigurationMappingWithIdBase<ChatGroup, int>
    {
        /// <inheritdoc/>
        public override void Configure(EntityTypeBuilder<ChatGroup> builder)
        {
            //Legacy comment translated to English.
            base.Configure(builder);

            builder.HasOne(z => z.AdminAgentTemplate)
                   .WithMany(z => z.AdminChatGroups)
                   .HasForeignKey(z => z.AdminAgentTemplateId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(z => z.EnterAgentTemplate)
                   .WithMany(z => z.EnterAgentChatGroups)
                   .HasForeignKey(z => z.EnterAgentTemplateId)
                   .OnDelete(DeleteBehavior.NoAction);

        }

    }
}
