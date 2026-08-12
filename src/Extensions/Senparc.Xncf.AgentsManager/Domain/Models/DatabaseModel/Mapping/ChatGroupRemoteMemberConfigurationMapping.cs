/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatGroupRemoteMemberConfigurationMapping.cs
    文件功能描述：数据模型、DTO 与映射定义


    创建标识：Senparc - 20260812

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

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
