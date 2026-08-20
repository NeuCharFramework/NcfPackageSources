/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AgentTemplateConfigurationMapping.cs
    文件功能描述：AgentTemplateConfigurationMapping 相关实现
    
    
    创建标识：Senparc - 20240616
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260804
    修改描述：v0.14.0-preview9 新增 Agent 模板知识库关联与管理统计

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Mapping
{
    [XncfAutoConfigurationMapping]
    public class AgentTemplateConfigurationMapping : ConfigurationMappingWithIdBase<AgentTemplate, int>
    {
        public override void Configure(EntityTypeBuilder<AgentTemplate> builder)
        {

            base.Configure(builder);
            builder.HasIndex(z => z.KnowledgeBaseId);
        }
    }
}
