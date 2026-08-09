using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharWorkflow.ACL;
using Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel;
using WorkflowEntity = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

public sealed class NeuCharWorkflowService : WorkflowClientServiceBase<WorkflowEntity>
{
    public NeuCharWorkflowService(INeuCharWorkflowRepository repository, IServiceProvider serviceProvider)
        : base(repository, serviceProvider) { }
}

public sealed class NeuCharWorkflowVersionService : WorkflowClientServiceBase<NeuCharWorkflowVersion>
{
    public NeuCharWorkflowVersionService(INeuCharWorkflowVersionRepository repository, IServiceProvider serviceProvider)
        : base(repository, serviceProvider) { }
}

public sealed class NeuCharWorkflowExecutionLogService : WorkflowClientServiceBase<NeuCharWorkflowExecutionLog>
{
    public NeuCharWorkflowExecutionLogService(INeuCharWorkflowExecutionLogRepository repository, IServiceProvider serviceProvider)
        : base(repository, serviceProvider) { }
}
