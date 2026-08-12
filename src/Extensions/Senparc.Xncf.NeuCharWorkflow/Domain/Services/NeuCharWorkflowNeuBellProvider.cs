/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowNeuBellProvider.cs
    文件功能描述：Workflow 发送的纽铃提醒与按任务回访消费


    创建标识：Senparc - 20260810

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.NeuBell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>“发送纽铃提醒”节点在点击任务后采用的消费方式。</summary>
public static class NeuCharWorkflowNeuBellConsumption
{
    public const string None = "none";
    public const string Item = "item";
    public const string Provider = "provider";

    public static string Normalize(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        Item => Item,
        Provider => Provider,
        _ => None
    };
}

/// <summary>
/// 仅保存当前 Host 进程内由 Workflow 节点发出的提醒。
/// 每条提醒链接到具体的 Workflow 运行任务，并可按节点配置选择不消费、消费本条或消费本订阅全部条目。
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
            Limit(title, 200, "Workflow 提醒"),
            Limit(summary, 4_000, "工作流已发送一条纽铃提醒。"),
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

    private static string? NormalizeRunId(string? runId) => Guid.TryParse(runId, out var parsed)
        ? parsed.ToString("N")
        : null;

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
