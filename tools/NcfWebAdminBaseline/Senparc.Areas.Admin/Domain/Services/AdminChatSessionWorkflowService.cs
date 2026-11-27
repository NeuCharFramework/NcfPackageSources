/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatSessionWorkflowService.cs
    File description: AdminChatSessionWorkflowService.cs related implementation

    Created by: Senparc - 20260821

    Modified by: Senparc - 20260822
    Change description: v0.6.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class AdminChatSessionWorkflowService : BaseClientService<AdminChatSessionWorkflow>
{
    /// <summary>
    /// Initializes persistence for workflow associations attached to administrator chat sessions.
    /// </summary>
    /// <param name="repository">The session-workflow repository.</param>
    /// <param name="serviceProvider">The dependency provider used by the base service.</param>
    public AdminChatSessionWorkflowService(
        IAdminChatSessionWorkflowRepository repository,
        IServiceProvider serviceProvider)
        : base(repository, serviceProvider)
    {
    }

    /// <summary>
    /// Lists workflows attached to a session in insertion order.
    /// </summary>
    /// <param name="sessionId">The chat session identifier.</param>
    /// <returns>The session workflow associations.</returns>
    public async Task<List<AdminChatSessionWorkflow>> GetSessionWorkflowsAsync(int sessionId)
    {
        var workflows = await GetFullListAsync(
            workflow => workflow.SessionId == sessionId,
            "AddedTime ASC");
        return workflows.ToList();
    }

    /// <summary>
    /// Adds valid workflow associations that are not already attached to the session.
    /// </summary>
    /// <param name="sessionId">The target chat session identifier.</param>
    /// <param name="workflows">Workflow IDs and display metadata to attach.</param>
    /// <returns>A task that completes after all missing associations are persisted.</returns>
    public async Task AddWorkflowsToSessionAsync(
        int sessionId,
        IEnumerable<(int id, string name, string description)> workflows)
    {
        foreach (var workflow in workflows ?? Enumerable.Empty<(int, string, string)>())
        {
            if (workflow.id <= 0)
            {
                continue;
            }

            var existing = await GetObjectAsync(item =>
                item.SessionId == sessionId &&
                item.WorkflowId == workflow.id);
            if (existing != null)
            {
                continue;
            }

            await SaveObjectAsync(new AdminChatSessionWorkflow(
                sessionId,
                workflow.id,
                workflow.name,
                workflow.description));
        }
    }

    /// <summary>
    /// Reconciles a session's workflow associations with the supplied desired set,
    /// adding missing items and deleting obsolete items.
    /// </summary>
    /// <param name="sessionId">The target chat session identifier.</param>
    /// <param name="workflows">The desired workflow associations.</param>
    /// <returns>A task that completes after reconciliation is persisted.</returns>
    public async Task SetSessionWorkflowsAsync(
        int sessionId,
        IEnumerable<(int id, string name, string description)> workflows)
    {
        var desired = (workflows ?? Enumerable.Empty<(int, string, string)>())
            .Where(workflow => workflow.id > 0)
            .GroupBy(workflow => workflow.id)
            .Select(group => group.First())
            .ToList();
        var desiredIds = desired.Select(workflow => workflow.id).ToHashSet();
        var existing = await GetSessionWorkflowsAsync(sessionId);

        await AddWorkflowsToSessionAsync(sessionId, desired);
        foreach (var obsolete in existing.Where(workflow => !desiredIds.Contains(workflow.WorkflowId)))
        {
            await DeleteObjectAsync(obsolete);
        }
    }
}
