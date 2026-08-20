/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatGroupConfigurationMapping.cs
    文件功能描述：ChatGroupConfigurationMapping 相关实现
    
    
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
    public class ChatGroupConfigurationMapping : ConfigurationMappingWithIdBase<ChatGroup, int>
    {
        public override void Configure(EntityTypeBuilder<ChatGroup> builder)
        {
            //throw new System.Exception("运行到这里了");
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
