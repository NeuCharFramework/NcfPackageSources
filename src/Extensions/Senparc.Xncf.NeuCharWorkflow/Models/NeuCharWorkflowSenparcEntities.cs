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
