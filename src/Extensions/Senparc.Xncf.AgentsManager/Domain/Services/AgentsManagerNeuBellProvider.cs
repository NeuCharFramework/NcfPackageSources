/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AgentsManagerNeuBellProvider.cs
    文件功能描述：AgentsManager Human 回合的 NeuBell 提醒

    创建标识：Senparc - 20260815
    修改描述：Human 回合创建提醒，成功回复后由业务路径消费

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

    private static string BuildTaskDetailUrl(Notification item)
    {
        return item.DetailUrl;
    }

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

    private static string NormalizeRecipient(string recipient)
        => string.IsNullOrWhiteSpace(recipient) ? "anonymous" : recipient.Trim();

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
