/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Database.cs
    File description: enhance workflow orchestration, replay, Webhook, and parallel execution

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20260913
    Change description: v0.4.0 add persistent index mappings for Chat messages

    Modified by: Senparc - 20260915
    Change description: v0.4.0 enhance Chat trigger message persistence and recovery

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
