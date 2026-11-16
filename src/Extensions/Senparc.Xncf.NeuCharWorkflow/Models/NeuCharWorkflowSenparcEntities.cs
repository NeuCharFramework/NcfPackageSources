/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: NeuCharWorkflowSenparcEntities.cs
    File description: 数据模型、DTO 与映射定义


    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

    Modified by: Senparc - 20260913
    Change description: v0.4.0 新增 Chat 消息持久化数据库上下文注册

    Modified by: Senparc - 20260915
    Change description: v0.4.0 增强 Chat 触发器消息持久化与恢复能力

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
    public DbSet<NeuCharWorkflowChatMessage> NeuCharWorkflowChatMessages { get; set; } = null!;
}
