/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: ChatGroupRemoteMemberConfigurationMapping.cs
    File description: data model, DTO, and mapping definitions


    Created by: Senparc - 20260812

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;

namespace Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Mapping
{
    [XncfAutoConfigurationMapping]
    public class ChatGroupRemoteMemberConfigurationMapping : ConfigurationMappingWithIdBase<ChatGroupRemoteMember, int>
    {
        public override void Configure(EntityTypeBuilder<ChatGroupRemoteMember> builder)
        {
            base.Configure(builder);
            builder.HasIndex(z => new { z.ChatGroupId, z.RemoteAgentId }).IsUnique();
            builder.HasIndex(z => z.ChatGroupId);
            builder.HasOne(z => z.RemoteAgent)
                .WithMany(z => z.ChatGroupRemoteMembers)
                .HasForeignKey(z => z.RemoteAgentId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
