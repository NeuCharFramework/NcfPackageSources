/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatGroupMemberConfigurationMapping.cs
    File description: ChatGroupMemberConfigurationMapping related implementation

    Created by: Senparc - 20240616

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;

namespace Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Mapping
{
    [XncfAutoConfigurationMapping]
    public class ChatGroupMemberConfigurationMapping : ConfigurationMappingWithIdBase<ChatGroupMember, int>
    {
        public override void Configure(EntityTypeBuilder<ChatGroupMember> builder)
        {
            base.Configure(builder);

            //Legacy comment translated to English.
            //builder.HasKey(e => new { e.ChatGroupId, e.AgentTemplateId });

            //builder.HasOne(z => z.ChatGroup)
            //    .WithMany(z => z.ChatGroupMembers)
            //    .HasForeignKey(z => z.ChatGroupId)
            //    .OnDelete(DeleteBehavior.NoAction);
        }

    }
}
