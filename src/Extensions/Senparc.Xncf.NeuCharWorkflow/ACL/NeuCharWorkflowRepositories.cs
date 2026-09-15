/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NeuCharWorkflowRepositories.cs
    文件功能描述：增强工作流编排、回放、Webhook 与并行执行能力


    创建标识：Senparc - 20260810

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

    修改标识：Senparc - 20260913
    修改描述：v0.4.0 新增 Chat 消息持久化仓储

    修改标识：Senparc - 20260915
    修改描述：v0.4.0 增强 Chat 触发器消息持久化与恢复能力

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
