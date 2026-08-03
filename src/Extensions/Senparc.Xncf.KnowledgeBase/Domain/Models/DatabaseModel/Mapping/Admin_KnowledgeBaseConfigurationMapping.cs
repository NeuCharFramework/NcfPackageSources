/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Admin_KnowledgeBaseConfigurationMapping.cs
    文件功能描述：Admin_KnowledgeBaseConfigurationMapping 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260804
    修改描述：v0.5.0-preview6 新增知识库生命周期管理与 Agent 模板集成

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel;

namespace Senparc.Xncf.KnowledgeBase.Models
{
    [XncfAutoConfigurationMapping]
    public class Admin_KnowledgeBaseConfigurationMapping : ConfigurationMappingWithIdBase<DatabaseModel.KnowledgeBase, int>
    {
        public override void Configure(EntityTypeBuilder<DatabaseModel.KnowledgeBase> builder)
        {
            base.Configure(builder);
        }
    }
}
