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
    private NeuCharWorkflowRepository() : base(null!) { }
    public NeuCharWorkflowRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public sealed class NeuCharWorkflowVersionRepository : ClientRepositoryBase<NeuCharWorkflowVersion>, INeuCharWorkflowVersionRepository
{
    private NeuCharWorkflowVersionRepository() : base(null!) { }
    public NeuCharWorkflowVersionRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public sealed class NeuCharWorkflowExecutionLogRepository : ClientRepositoryBase<NeuCharWorkflowExecutionLog>, INeuCharWorkflowExecutionLogRepository
{
    private NeuCharWorkflowExecutionLogRepository() : base(null!) { }
    public NeuCharWorkflowExecutionLogRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public sealed class NeuCharWorkflowChatMessageRepository : ClientRepositoryBase<NeuCharWorkflowChatMessage>, INeuCharWorkflowChatMessageRepository
{
    private NeuCharWorkflowChatMessageRepository() : base(null!) { }
    public NeuCharWorkflowChatMessageRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}
