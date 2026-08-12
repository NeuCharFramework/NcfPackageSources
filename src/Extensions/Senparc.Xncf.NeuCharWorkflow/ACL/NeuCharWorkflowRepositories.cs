/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NeuCharWorkflowRepositories.cs
    文件功能描述：增强工作流编排、回放、Webhook 与并行执行能力


    创建标识：Senparc - 20260810

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

----------------------------------------------------------------*/

using Senparc.Ncf.Repository;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel;
using WorkflowEntity = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow;

namespace Senparc.Xncf.NeuCharWorkflow.ACL;

public interface INeuCharWorkflowRepository : IClientRepositoryBase<WorkflowEntity> { }
public interface INeuCharWorkflowVersionRepository : IClientRepositoryBase<NeuCharWorkflowVersion> { }
public interface INeuCharWorkflowExecutionLogRepository : IClientRepositoryBase<NeuCharWorkflowExecutionLog> { }

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
