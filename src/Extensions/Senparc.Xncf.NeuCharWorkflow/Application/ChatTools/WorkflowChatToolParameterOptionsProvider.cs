/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: WorkflowChatToolParameterOptionsProvider.cs
    File description: Provides workflow chat-tool parameter options.

    Created by: Senparc - 20261125

    Modified by: Senparc - 20261125
    Change description: v0.4.8 Added chat-tool parameter option integration.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.XncfBase.ChatTools;
using Senparc.Xncf.NeuCharWorkflow.Application.AppServices;

namespace Senparc.Xncf.NeuCharWorkflow.Application.ChatTools;

public sealed class WorkflowChatToolParameterOptionsProvider(
    NeuCharWorkflowAppService workflowAppService,
    IAdminWorkContextProvider adminWorkContextProvider) : IChatToolParameterOptionsProvider
{
    public async Task<IReadOnlyCollection<ChatToolParameterOption>> GetOptionsAsync(
        ChatToolParameterOptionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
        var query = request.Query?.Trim();
        var workflows = await workflowAppService.GetListAsync(userId).ConfigureAwait(false);

        return workflows
            .Where(workflow => string.IsNullOrWhiteSpace(query)
                               || workflow.Id.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
                               || workflow.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                               || workflow.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
            .Take(Math.Clamp(request.Limit, 1, 100))
            .Select(workflow => new ChatToolParameterOption
            {
                Value = workflow.Id.ToString(),
                Label = workflow.Name,
                Description = string.IsNullOrWhiteSpace(workflow.Description)
                    ? $"Workflow ID: {workflow.Id} · Rev. {workflow.Revision}"
                    : $"#{workflow.Id} · Rev. {workflow.Revision} · {workflow.Description}",
                Keywords = $"{workflow.Id} {workflow.Name} {workflow.Description} {workflow.TriggerType}"
            })
            .ToArray();
    }
}
