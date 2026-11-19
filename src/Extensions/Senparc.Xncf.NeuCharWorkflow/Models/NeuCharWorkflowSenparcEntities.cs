/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowSenparcEntities.cs
    File description: data model, DTO, and mapping definitions

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20260913
    Change description: v0.4.0 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.4.0 enhance Chat trigger message persistence and recovery

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

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
