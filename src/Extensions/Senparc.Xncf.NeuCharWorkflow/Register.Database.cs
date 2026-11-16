/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: Register.Database.cs
    File description: 增强工作流编排、回放、Webhook 与并行执行能力


    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

    Modified by: Senparc - 20260913
    Change description: v0.4.0 新增 Chat 消息持久化索引映射

    Modified by: Senparc - 20260915
    Change description: v0.4.0 增强 Chat 触发器消息持久化与恢复能力

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using System;
using Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel;
using WorkflowEntity = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow;

namespace Senparc.Xncf.NeuCharWorkflow;

public partial class Register : IXncfDatabase
{
    public const string DATABASE_PREFIX = "NEUCHAR_WORKFLOW_";

    public string DatabaseUniquePrefix => DATABASE_PREFIX;

    public Type TryGetXncfDatabaseDbContextType =>
        MultipleDatabasePool.Instance.GetXncfDbContextType(this);

    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowEntity>()
            .HasIndex(z => z.LegacySourceKey)
            .IsUnique();
        modelBuilder.Entity<NeuCharWorkflowVersion>()
            .HasIndex(z => z.LegacySourceKey)
            .IsUnique();
        modelBuilder.Entity<NeuCharWorkflowVersion>()
            .HasIndex(z => new { z.WorkflowId, z.Revision });
        modelBuilder.Entity<NeuCharWorkflowExecutionLog>()
            .HasIndex(z => new { z.WorkflowId, z.StartedAt });
        modelBuilder.Entity<NeuCharWorkflowChatMessage>()
            .HasIndex(z => new { z.WorkflowId, z.ParticipantKeyHash });
    }

    public void AddXncfDatabaseModule(IServiceCollection services)
    {
    }
}
