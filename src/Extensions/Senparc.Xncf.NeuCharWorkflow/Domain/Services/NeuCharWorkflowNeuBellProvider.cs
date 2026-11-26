/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowNeuBellProvider.cs
    File description: Workflow notifications and task-based NeuBell consumption

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.NeuBell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>Consumption mode used by a Send NeuBell node after a task is opened.</summary>
public static class NeuCharWorkflowNeuBellConsumption
{
    public const string None = "none";
    public const string Item = "item";
    public const string Provider = "provider";

    /// <summary>
    /// Normalizes an external consumption-mode value to one of the supported constants.
    /// </summary>
    /// <param name="value">The requested mode, or <see langword="null"/>.</param>
    /// <returns><see cref="Item"/>, <see cref="Provider"/>, or <see cref="None"/>.</returns>
    public static string Normalize(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        Item => Item,
        Provider => Provider,
        _ => None
    };
}

/// <summary>
/// Stores only notifications emitted by Workflow nodes in the current host process.
/// Each notification links to a Workflow run and can consume none, this item, or all
/// items in the subscription according to the node configuration.
/// </summary>
public sealed class NeuCharWorkflowNeuBellProvider : INeuBellProvider, INeuBellConsumableProvider
{
    public const string ProviderIdValue = "neuchar-workflow";
    private const int Capacity = 200;
    private readonly object _syncRoot = new();
    private readonly List<Notification> _notifications = new();

    private sealed record Notification(
        string Id,
        string RecipientUserId,
        int WorkflowId,
        string WorkflowName,
        string? RunId,
        string NodeId,
        string Title,
        string Summary,
        string ConsumeMode,
        DateTimeOffset CreatedAt);

    public string ProviderId => ProviderIdValue;

    public string ModuleUid => new Register().Uid;

    /// <summary>
    /// Stores a workflow notification for the specified administrator and returns its item identifier.
    /// The in-memory provider keeps only the newest notifications up to its fixed capacity.
    /// </summary>
    /// <param name="adminUserId">The administrator receiving the notification.</param>
    /// <param name="workflowId">The workflow identifier associated with the notification.</param>
    /// <param name="workflowName">The workflow display name.</param>
    /// <param name="runId">The optional workflow run identifier.</param>
    /// <param name="nodeId">The node that emitted the notification.</param>
    /// <param name="title">The notification title.</param>
    /// <param name="summary">The notification summary.</param>
    /// <param name="consumeMode">The requested consumption mode.</param>
    /// <returns>The generated notification item identifier.</returns>
    public string Send(
        int adminUserId,
        int workflowId,
        string workflowName,
        string? runId,
        string nodeId,
        string? title,
        string? summary,
        string? consumeMode)
    {
        var notification = new Notification(
            Guid.NewGuid().ToString("N"),
            adminUserId.ToString(),
            workflowId,
            Limit(workflowName, 200, "Workflow"),
            NormalizeRunId(runId),
            Limit(nodeId, 200, "neubell"),
            Limit(title, 200, "Workflow notification"),
            Limit(summary, 4_000, "The Workflow sent a NeuBell notification."),
            NeuCharWorkflowNeuBellConsumption.Normalize(consumeMode),
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
    /// Consumes one notification when it belongs to the requesting administrator.
    /// </summary>
    /// <param name="context">The authenticated NeuBell request context.</param>
    /// <param name="itemId">The notification item identifier.</param>
    /// <param name="cancellationToken">Token used to cancel consumption.</param>
    /// <returns>One when an item was removed; otherwise zero.</returns>
    public ValueTask<int> ConsumeItemAsync(
        NeuBellRequestContext context,
        string itemId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return ValueTask.FromResult(0);
        }

        lock (_syncRoot)
        {
            var index = _notifications.FindIndex(item =>
                string.Equals(item.Id, itemId, StringComparison.Ordinal) &&
                string.Equals(item.RecipientUserId, context.UserId, StringComparison.Ordinal));
            if (index < 0)
            {
                return ValueTask.FromResult(0);
            }
            _notifications.RemoveAt(index);
            return ValueTask.FromResult(1);
        }
    }

    /// <summary>
    /// Consumes all notifications belonging to the requesting administrator.
    /// </summary>
    /// <param name="context">The authenticated NeuBell request context.</param>
    /// <param name="cancellationToken">Token used to cancel consumption.</param>
    /// <returns>The number of removed notifications.</returns>
    public ValueTask<int> ConsumeAllAsync(
        NeuBellRequestContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot)
        {
            var consumed = _notifications.RemoveAll(item =>
                string.Equals(item.RecipientUserId, context.UserId, StringComparison.Ordinal));
            return ValueTask.FromResult(consumed);
        }
    }

    /// <summary>
    /// Returns the current notification snapshot for the requesting administrator.
    /// </summary>
    /// <param name="context">The authenticated NeuBell request context.</param>
    /// <param name="cancellationToken">Token used to cancel snapshot creation.</param>
    /// <returns>The provider snapshot containing visible workflow notifications.</returns>
    public ValueTask<NeuBellSnapshot> GetSnapshotAsync(
        NeuBellRequestContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<NeuBellItem> items;
        lock (_syncRoot)
        {
            items = _notifications
                .Where(item => string.Equals(item.RecipientUserId, context.UserId, StringComparison.Ordinal))
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
            "Workflow",
            "fa fa-random",
            true,
            items));
    }

    /// <summary>
    /// Builds the administration task URL containing provider, item, run, and consume metadata.
    /// </summary>
    /// <param name="item">The notification represented by the URL.</param>
    /// <returns>The encoded administration task URL.</returns>
    private static string BuildTaskDetailUrl(Notification item)
    {
        var query = new List<string>
        {
            $"workflowId={item.WorkflowId}",
            $"neuBellProvider={Uri.EscapeDataString(ProviderIdValue)}",
            $"neuBellItem={Uri.EscapeDataString(item.Id)}",
            $"neuBellConsume={Uri.EscapeDataString(item.ConsumeMode)}"
        };
        if (!string.IsNullOrWhiteSpace(item.RunId))
        {
            query.Insert(1, $"runId={Uri.EscapeDataString(item.RunId)}");
        }
        return "/Admin/NeuCharWorkflow/Tasks?" + string.Join("&", query);
    }

    /// <summary>
    /// Normalizes a run identifier to compact GUID text.
    /// </summary>
    /// <param name="runId">The candidate run identifier.</param>
    /// <returns>The compact identifier, or <see langword="null"/> when invalid.</returns>
    private static string? NormalizeRunId(string? runId) => Guid.TryParse(runId, out var parsed)
        ? parsed.ToString("N")
        : null;

    /// <summary>
    /// Trims notification text and applies a fallback or maximum length.
    /// </summary>
    /// <param name="value">The candidate notification text.</param>
    /// <param name="maxLength">The maximum permitted length.</param>
    /// <param name="fallback">The value used for blank input.</param>
    /// <returns>The normalized bounded text.</returns>
    private static string Limit(string? value, int maxLength, string fallback)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return fallback;
        }
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}
