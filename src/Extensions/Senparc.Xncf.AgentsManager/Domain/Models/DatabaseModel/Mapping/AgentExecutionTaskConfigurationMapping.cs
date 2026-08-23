/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AgentExecutionTaskConfigurationMapping.cs
    文件功能描述：独立 Agent 执行任务数据库映射

    创建标识：Senparc - 20260822

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 统一独立 Agent 执行记录

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 新增独立 Agent 执行任务持久化、管理页和 SSE 过程回放


----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Mapping;

[XncfAutoConfigurationMapping]
public sealed class AgentExecutionTaskConfigurationMapping
    : ConfigurationMappingWithIdBase<AgentExecutionTask, int>
{
    public override void Configure(EntityTypeBuilder<AgentExecutionTask> builder)
    {
        base.Configure(builder);
        builder.HasIndex(item => new { item.AgentTemplateId, item.StartTime });
        builder.HasIndex(item => item.CorrelationId);
        builder.HasOne(item => item.AgentTemplate)
            .WithMany()
            .HasForeignKey(item => item.AgentTemplateId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
