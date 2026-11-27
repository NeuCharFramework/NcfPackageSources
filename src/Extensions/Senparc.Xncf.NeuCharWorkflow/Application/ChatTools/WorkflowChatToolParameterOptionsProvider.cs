/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: WorkflowChatToolParameterOptionsProvider.cs
    File description: Provides workflow chat-tool parameter options.

    Created by: Senparc - 20261125

    Modified by: Senparc - 20261125
    Change description: v0.4.8 Added chat-tool parameter option integration.

    Modified by: Senparc - 20261127
    Change description: v0.5.0.0 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.XncfBase.ChatTools;
using Senparc.Xncf.NeuCharWorkflow.Application.AppServices;

namespace Senparc.Xncf.NeuCharWorkflow.Application.ChatTools;

/// <summary>
/// Supplies administrator-scoped workflow choices for chat-tool parameter editors.
/// </summary>
public sealed class WorkflowChatToolParameterOptionsProvider(
    NeuCharWorkflowAppService workflowAppService,
    IAdminWorkContextProvider adminWorkContextProvider) : IChatToolParameterOptionsProvider
{
    /// <summary>
    /// Initializes the workflow option provider with the workflow service and administrator context provider.
    /// </summary>
    /// <param name="workflowAppService">The service used to load owner-scoped workflows.</param>
    /// <param name="adminWorkContextProvider">The provider used to identify the current administrator.</param>
    /// <summary>
    /// Returns workflow options visible to the current administrator, filtered by query and bounded by the request limit.
    /// </summary>
    /// <param name="request">The query text and maximum option count.</param>
    /// <param name="cancellationToken">Token used to cancel option loading.</param>
    /// <returns>Workflow options suitable for a chat-tool parameter selector.</returns>
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
