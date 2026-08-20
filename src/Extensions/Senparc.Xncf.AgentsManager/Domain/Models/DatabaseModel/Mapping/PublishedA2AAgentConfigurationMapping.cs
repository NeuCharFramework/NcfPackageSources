/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PublishedA2AAgentConfigurationMapping.cs
    文件功能描述：数据模型、DTO 与映射定义


    创建标识：Senparc - 20260812

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;

namespace Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Mapping
{
    [XncfAutoConfigurationMapping]
    public class PublishedA2AAgentConfigurationMapping : ConfigurationMappingWithIdBase<PublishedA2AAgent, int>
    {
        public override void Configure(EntityTypeBuilder<PublishedA2AAgent> builder)
        {
            base.Configure(builder);
            builder.HasIndex(z => z.AgentTemplateId).IsUnique();
            builder.HasIndex(z => z.PublicAgentKey).IsUnique();
            builder.HasIndex(z => z.Enable);
        }
    }
}
