/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatGroupMemberConfigurationMapping.cs
    文件功能描述：ChatGroupMemberConfigurationMapping 相关实现
    
    
    创建标识：Senparc - 20240616
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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

            //联合主键
            //builder.HasKey(e => new { e.ChatGroupId, e.AgentTemplateId });

            //builder.HasOne(z => z.ChatGroup)
            //    .WithMany(z => z.ChatGroupMembers)
            //    .HasForeignKey(z => z.ChatGroupId)
            //    .OnDelete(DeleteBehavior.NoAction);
        }

    }
}
