/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentsManagerNeuBellProvider.cs
    File description: Related implementation.
    Change description: Updated module behavior and related support.

    Created by: Senparc - 20260815

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.NeuBell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

public sealed class AgentsManagerNeuBellProvider : INeuBellProvider, INeuBellConsumableProvider
{
    public const string ProviderIdValue = "agents-manager";
    private const int Capacity = 200;
    private readonly object _syncRoot = new();
    private readonly List<Notification> _notifications = new();

    private sealed record Notification(
        string Id,
        string RecipientUserId,
        int ChatTaskId,
        string AgentName,
        string Title,
        string Summary,
        string DetailUrl,
        DateTimeOffset CreatedAt);

    public string ProviderId => ProviderIdValue;
    public string ModuleUid => new Register().Uid;

    /// <summary>
    /// Creates a Human participant notification for a ChatTask.
    /// </summary>
    /// <param name="chatTaskId">The ChatTask identifier.</param>
    /// <param name="recipientUserId">The notification recipient.</param>
    /// <param name="agentName">The Agent awaiting input.</param>
    /// <returns>The consumable notification identifier.</returns>
    public string Send(int chatTaskId, string recipientUserId, string agentName)
    {
        return Add(
            chatTaskId,
            recipientUserId,
            agentName,
            "AgentsManager 等待人工回复",
            $"任务 #{chatTaskId} 正在等待 {Limit(agentName, 120, HumanParticipantConstants.Name)} 输入文本。",
            $"/Admin/AgentsManager/Index#tab=third&taskId={chatTaskId}");
    }

    /// <summary>
    /// Creates a tool-approval notification for a ChatTask.
    /// </summary>
    /// <param name="chatTaskId">The ChatTask identifier.</param>
    /// <param name="recipientUserId">The notification recipient.</param>
    /// <param name="agentName">The requesting Agent.</param>
    /// <param name="toolName">The requested tool name.</param>
    /// <returns>The consumable notification identifier.</returns>
    public string SendToolApproval(
        int chatTaskId,
        string recipientUserId,
        string agentName,
        string toolName)
    {
        return Add(
            chatTaskId,
            recipientUserId,
            agentName,
            "AgentsManager 等待工具审批",
            $"任务 #{chatTaskId} 的 {Limit(agentName, 120, "Agent")} 请求调用 {Limit(toolName, 120, "工具")}。",
            $"/Admin/AgentsManager/Index#tab=third&taskId={chatTaskId}");
    }

    /// <summary>
    /// Creates a tool-approval notification linked to a Workflow run correlation ID.
    /// </summary>
    /// <param name="correlationId">The Workflow run correlation ID.</param>
    /// <param name="recipientUserId">The notification recipient.</param>
    /// <param name="agentName">The requesting Agent.</param>
    /// <param name="toolName">The requested tool name.</param>
    /// <returns>The consumable notification identifier.</returns>
    public string SendWorkflowToolApproval(
        string correlationId,
        string recipientUserId,
        string agentName,
        string toolName)
    {
        return Add(
            0,
            recipientUserId,
            agentName,
            "Workflow 等待工具审批",
            $"{Limit(agentName, 120, "Agent")} 请求调用 {Limit(toolName, 120, "工具")}。",
            BuildWorkflowRunUrl(correlationId));
    }

    /// <summary>
    /// Adds a bounded notification to the in-memory provider queue.
    /// </summary>
    /// <param name="chatTaskId">The ChatTask identifier, or zero for Workflow notifications.</param>
    /// <param name="recipientUserId">The notification recipient.</param>
    /// <param name="agentName">The Agent name.</param>
    /// <param name="title">The notification title.</param>
    /// <param name="summary">The notification summary.</param>
    /// <param name="detailUrl">The client detail URL.</param>
    /// <returns>The notification identifier.</returns>
    private string Add(
        int chatTaskId,
        string recipientUserId,
        string agentName,
        string title,
        string summary,
        string detailUrl)
    {
        var notification = new Notification(
            Guid.NewGuid().ToString("N"),
            NormalizeRecipient(recipientUserId),
            chatTaskId,
            Limit(agentName, 120, HumanParticipantConstants.Name),
            title,
            summary,
            detailUrl,
            DateTimeOffset.UtcNow);

        lock (_syncRoot)
        {
            _notifications.Add(notification);
            if (_notifications.Count > Capacity)
            {
                _notifications.RemoveRange(0, _notifications.Count - Capacity);
            }
        }

        return notification.Id;
    }

    /// <summary>
    /// Consumes one notification only when its recipient matches the request context.
    /// </summary>
    /// <param name="context">The consuming user context.</param>
    /// <param name="itemId">The notification identifier.</param>
    /// <param name="cancellationToken">Token used to cancel consumption.</param>
    /// <returns>A value task containing the number of consumed items.</returns>
    public ValueTask<int> ConsumeItemAsync(
        NeuBellRequestContext context,
        string itemId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(itemId) || context == null)
        {
            return ValueTask.FromResult(0);
        }

        lock (_syncRoot)
        {
            var index = _notifications.FindIndex(item =>
                string.Equals(item.Id, itemId, StringComparison.Ordinal)
                && string.Equals(item.RecipientUserId, NormalizeRecipient(context.UserId), StringComparison.Ordinal));
            if (index < 0)
            {
                return ValueTask.FromResult(0);
            }

            _notifications.RemoveAt(index);
            return ValueTask.FromResult(1);
        }
    }

    /// <summary>
    /// Consumes all notifications belonging to the request-context user.
    /// </summary>
    /// <param name="context">The consuming user context.</param>
    /// <param name="cancellationToken">Token used to cancel consumption.</param>
    /// <returns>A value task containing the consumed count.</returns>
    public ValueTask<int> ConsumeAllAsync(
        NeuBellRequestContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context == null)
        {
            return ValueTask.FromResult(0);
        }

        lock (_syncRoot)
        {
            var consumed = _notifications.RemoveAll(item =>
                string.Equals(item.RecipientUserId, NormalizeRecipient(context.UserId), StringComparison.Ordinal));
            return ValueTask.FromResult(consumed);
        }
    }

    /// <summary>
    /// Returns the current recipient-filtered NeuBell snapshot in newest-first order.
    /// </summary>
    /// <param name="context">The requesting user context.</param>
    /// <param name="cancellationToken">Token used to cancel snapshot creation.</param>
    /// <returns>A value task containing the provider snapshot.</returns>
    public ValueTask<NeuBellSnapshot> GetSnapshotAsync(
        NeuBellRequestContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var recipient = NormalizeRecipient(context?.UserId);
        IReadOnlyList<NeuBellItem> items;
        lock (_syncRoot)
        {
            items = _notifications
                .Where(item => string.Equals(item.RecipientUserId, recipient, StringComparison.Ordinal))
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new NeuBellItem(
                    item.Id,
                    item.Title,
                    item.Summary,
                    1,
                    "warning",
                    BuildTaskDetailUrl(item),
                    item.CreatedAt))
                .ToList();
        }

        return ValueTask.FromResult(new NeuBellSnapshot(
            ProviderId,
            ModuleUid,
            "AgentsManager",
            "fa fa-users",
            true,
            items));
    }

    /// <summary>Returns the detail URL stored on a notification.</summary>
    /// <param name="item">The notification.</param>
    /// <returns>The detail URL.</returns>
    private static string BuildTaskDetailUrl(Notification item)
    {
        return item.DetailUrl;
    }

    /// <summary>
    /// Converts a Workflow correlation ID to its task detail URL, or returns the generic task page.
    /// </summary>
    /// <param name="correlationId">The Workflow correlation ID.</param>
    /// <returns>The safe detail URL.</returns>
    private static string BuildWorkflowRunUrl(string correlationId)
    {
        const string prefix = "workflow-";
        const string separator = "-run-";
        var normalized = correlationId?.Trim() ?? string.Empty;
        var separatorIndex = normalized.IndexOf(separator, prefix.Length, StringComparison.Ordinal);
        if (!normalized.StartsWith(prefix, StringComparison.Ordinal)
            || separatorIndex <= prefix.Length
            || !int.TryParse(normalized[prefix.Length..separatorIndex], out var workflowId)
            || workflowId <= 0
            || !Guid.TryParseExact(normalized[(separatorIndex + separator.Length)..], "N", out var runId))
        {
            return "/Admin/NeuCharWorkflow/Tasks";
        }

        return $"/Admin/NeuCharWorkflow/Index?workflowId={workflowId}&runId={runId:N}";
    }

    /// <summary>Normalizes an empty recipient to the anonymous bucket.</summary>
    /// <param name="recipient">The recipient identifier.</param>
    /// <returns>The normalized recipient.</returns>
    private static string NormalizeRecipient(string recipient)
        => string.IsNullOrWhiteSpace(recipient) ? "anonymous" : recipient.Trim();

    /// <summary>Trims and bounds notification text, applying a fallback for blank input.</summary>
    /// <param name="value">The source text.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <param name="fallback">The fallback text.</param>
    /// <returns>The bounded text.</returns>
    private static string Limit(string value, int maxLength, string fallback)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return fallback;
        }

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}
