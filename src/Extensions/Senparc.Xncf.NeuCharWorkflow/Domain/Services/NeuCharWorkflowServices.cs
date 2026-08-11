using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharWorkflow.ACL;
using Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel;
using WorkflowEntity = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow;
using System;
using System.Linq;

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

    public async Task<NeuCharWorkflowExecutionLog?> GetLatestReplaySnapshotAsync(int workflowId)
    {
        var logs = await GetFullListAsync(
            log => log.WorkflowId == workflowId,
            log => log.StartedAt,
            OrderingType.Descending).ConfigureAwait(false);
        return logs.FirstOrDefault(log => !string.IsNullOrWhiteSpace(log.ReplaySnapshotJson));
    }

    public async Task<IReadOnlyList<NeuCharWorkflowExecutionLog>> GetRecentCompletedAsync(int workflowId)
    {
        var logs = await GetFullListAsync(log => log.WorkflowId == workflowId,
            log => log.StartedAt, OrderingType.Descending).ConfigureAwait(false);
        return logs.Where(log => log.Succeeded == true).Take(20).ToList();
    }

    public async Task<NeuCharWorkflowExecutionLog?> GetReplaySnapshotAsync(int workflowId, string snapshotHash)
    {
        if (string.IsNullOrWhiteSpace(snapshotHash))
        {
            return null;
        }

        var logs = await GetFullListAsync(
            log => log.WorkflowId == workflowId,
            log => log.StartedAt,
            OrderingType.Descending).ConfigureAwait(false);
        return logs.FirstOrDefault(log =>
            string.Equals(log.ReplaySnapshotHash, snapshotHash, StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(log.ReplaySnapshotJson));
    }
}
