using System;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Ncf.XncfBase.ChatTools;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Services;

namespace Senparc.Xncf.AgentsManager.Application.ChatTools;

public sealed class AgentTaskChatToolAsyncActionMonitor(
    ChatTaskService chatTaskService,
    HumanInTheLoopRequestStore humanRequests) : IChatToolAsyncActionMonitor
{
    public bool Supports(ChatToolAsyncAction action)
    {
        return action != null
            && string.Equals(action.ActionType, "agents-task", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ChatToolAsyncActionState> GetStateAsync(
        ChatToolAsyncAction action,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(action.Target, out var taskId) || taskId <= 0)
        {
            throw new ArgumentException("Invalid Agent ChatTask target.", nameof(action));
        }

        var task = await chatTaskService.GetObjectAsync(item => item.Id == taskId)
            .ConfigureAwait(false);
        if (task == null)
        {
            return new ChatToolAsyncActionState(
                action.ActionType, action.Target, "failed", true,
                $"ChatTask #{taskId} was not found.", null);
        }

        var pendingHuman = humanRequests.GetPending(taskId).Count;
        var terminal = task.Status is ChatTask_Status.Finished
            or ChatTask_Status.Cancelled
            or ChatTask_Status.Failed;
        var status = task.Status switch
        {
            ChatTask_Status.Finished => "completed",
            ChatTask_Status.Cancelled => "cancelled",
            ChatTask_Status.Failed => "failed",
            _ => pendingHuman > 0 || task.Status == ChatTask_Status.Paused
                ? "waiting-human"
                : "running"
        };
        return new ChatToolAsyncActionState(
            action.ActionType,
            action.Target,
            status,
            terminal,
            task.Status.ToString(),
            task.ResultComment,
            pendingHuman);
    }
}
