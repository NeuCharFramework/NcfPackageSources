/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowRepositories.cs
    File description: enhance workflow orchestration, replay, Webhook, and parallel execution

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

using Senparc.Ncf.Repository;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel;
using WorkflowEntity = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow;

namespace Senparc.Xncf.NeuCharWorkflow.ACL;

public interface INeuCharWorkflowRepository : IClientRepositoryBase<WorkflowEntity> { }
public interface INeuCharWorkflowVersionRepository : IClientRepositoryBase<NeuCharWorkflowVersion> { }
public interface INeuCharWorkflowExecutionLogRepository : IClientRepositoryBase<NeuCharWorkflowExecutionLog> { }
public interface INeuCharWorkflowChatMessageRepository : IClientRepositoryBase<NeuCharWorkflowChatMessage> { }

public sealed class NeuCharWorkflowRepository : ClientRepositoryBase<WorkflowEntity>, INeuCharWorkflowRepository
{
    /// <summary>
    /// Initializes an ORM-only constructor for repository infrastructure.
    /// </summary>
    private NeuCharWorkflowRepository() : base(null!) { }
    /// <summary>
    /// Initializes the workflow repository over the NCF database boundary.
    /// </summary>
    /// <param name="ncfDbData">The NCF database abstraction.</param>
    public NeuCharWorkflowRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public sealed class NeuCharWorkflowVersionRepository : ClientRepositoryBase<NeuCharWorkflowVersion>, INeuCharWorkflowVersionRepository
{
    /// <summary>
    /// Initializes an ORM-only constructor for repository infrastructure.
    /// </summary>
    private NeuCharWorkflowVersionRepository() : base(null!) { }
    /// <summary>
    /// Initializes the workflow-version repository over the NCF database boundary.
    /// </summary>
    /// <param name="ncfDbData">The NCF database abstraction.</param>
    public NeuCharWorkflowVersionRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public sealed class NeuCharWorkflowExecutionLogRepository : ClientRepositoryBase<NeuCharWorkflowExecutionLog>, INeuCharWorkflowExecutionLogRepository
{
    /// <summary>
    /// Initializes an ORM-only constructor for repository infrastructure.
    /// </summary>
    private NeuCharWorkflowExecutionLogRepository() : base(null!) { }
    /// <summary>
    /// Initializes the execution-log repository over the NCF database boundary.
    /// </summary>
    /// <param name="ncfDbData">The NCF database abstraction.</param>
    public NeuCharWorkflowExecutionLogRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public sealed class NeuCharWorkflowChatMessageRepository : ClientRepositoryBase<NeuCharWorkflowChatMessage>, INeuCharWorkflowChatMessageRepository
{
    /// <summary>
    /// Initializes an ORM-only constructor for repository infrastructure.
    /// </summary>
    private NeuCharWorkflowChatMessageRepository() : base(null!) { }
    /// <summary>
    /// Initializes the chat-message repository over the NCF database boundary.
    /// </summary>
    /// <param name="ncfDbData">The NCF database abstraction.</param>
    public NeuCharWorkflowChatMessageRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}
