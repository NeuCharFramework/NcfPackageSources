using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.XncfBase.ChatTools;
using Senparc.Xncf.NeuCharWorkflow.Application.AppServices;

namespace Senparc.Xncf.NeuCharWorkflow.Application.ChatTools;

public sealed class WorkflowRunChatToolAsyncActionMonitor(
    NeuCharWorkflowAppService workflowAppService,
    IAdminWorkContextProvider adminWorkContextProvider) : IChatToolAsyncActionMonitor
{
    public bool Supports(ChatToolAsyncAction action)
    {
        return action != null
            && string.Equals(action.ActionType, "workflow-run", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ChatToolAsyncActionState> GetStateAsync(
        ChatToolAsyncAction action,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(action.Target, out var runId) || runId == Guid.Empty)
        {
            throw new ArgumentException("Invalid Workflow run target.", nameof(action));
        }

        var userId = adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
        var snapshot = await workflowAppService
            .GetRunStatusAsync(runId, userId, 0, cancellationToken)
            .ConfigureAwait(false);
        if (snapshot == null)
        {
            return new ChatToolAsyncActionState(
                action.ActionType, action.Target, "failed", true,
                "Workflow run was not found or is not accessible.", null);
        }

        var pendingHuman = snapshot.HumanInteractions?.Count ?? 0;
        var terminal = !snapshot.Running;
        var status = snapshot.Running
            ? pendingHuman > 0 ? "waiting-human" : "running"
            : snapshot.Succeeded == true ? "completed"
                : snapshot.Succeeded == false ? "failed"
                    : "cancelled";
        return new ChatToolAsyncActionState(
            action.ActionType,
            action.Target,
            status,
            terminal,
            snapshot.ErrorMessage ?? snapshot.Events.LastOrDefault()?.Message,
            snapshot.FinalOutput,
            pendingHuman);
    }
}
