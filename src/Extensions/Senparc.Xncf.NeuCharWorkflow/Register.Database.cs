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
    }

    public void AddXncfDatabaseModule(IServiceCollection services)
    {
    }
}
