/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NeuCharWorkflowSenparcEntities.cs
    文件功能描述：数据模型、DTO 与映射定义


    创建标识：Senparc - 20260810

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel;
using WorkflowEntity = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow;

namespace Senparc.Xncf.NeuCharWorkflow.Models;

public class NeuCharWorkflowSenparcEntities : XncfDatabaseDbContext
{
    public NeuCharWorkflowSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions) { }

    public DbSet<WorkflowEntity> NeuCharWorkflows { get; set; } = null!;
    public DbSet<NeuCharWorkflowVersion> NeuCharWorkflowVersions { get; set; } = null!;
    public DbSet<NeuCharWorkflowExecutionLog> NeuCharWorkflowExecutionLogs { get; set; } = null!;
}
