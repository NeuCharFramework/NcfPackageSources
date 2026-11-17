/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuBellTestProvider.cs
    File description: Related implementation.
    Legacy comment translated to English.

    Created by: Senparc - 20260805

    Modified by: Senparc - 20260808
    Change description: v0.4.0 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.5.0 Updated module behavior and related support.

    Modified by: Senparc - 20260914
    Change description: v0.7.1 add SendAndGetItem（sendandreturn NeuBellItem），

    Modified by: Senparc - 20260916
    Change description: v0.9.0 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.NeuBell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed class NeuBellTestProvider : INeuBellProvider, INeuBellConsumableProvider
{
    public const string ProviderIdValue = "admin-neubell-test";
    public const string ItemIdPrefix = "function-reminder-";
    public const string ItemTitle = "NeuBell 测试提醒";
    public const string ItemSummary = "由 Function 发送；可在 Function 中消费本条，或清除当前订阅全部提醒。";
    public const string ItemSeverity = "warning";
    public const string ItemDetailUrl = "/Admin/Index";

    private readonly object _syncRoot = new();
    private readonly List<Reminder> _pendingReminders = new();
    private int _sequence;

    private sealed record Reminder(string Id, DateTimeOffset CreatedAt);

    private static NeuBellItem ToNeuBellItem(Reminder reminder)
    {
        return new NeuBellItem(
            reminder.Id,
            ItemTitle,
            ItemSummary,
            1,
            ItemSeverity,
            ItemDetailUrl,
            reminder.CreatedAt);
    }

    public string ProviderId => ProviderIdValue;

    public string ModuleUid => Register.ModuleUid;

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public int PendingCount
    {
        get
        {
            lock (_syncRoot)
            {
                return _pendingReminders.Count;
            }
        }
    }

    public int Send()
    {
        lock (_syncRoot)
        {
            if (_pendingReminders.Count < int.MaxValue)
            {
                _pendingReminders.Add(new Reminder(
                    ItemIdPrefix + ++_sequence,
                    DateTimeOffset.Now));
            }
            return _pendingReminders.Count;
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public NeuBellItem SendAndGetItem()
    {
        Reminder reminder;
        lock (_syncRoot)
        {
            reminder = new Reminder(
                ItemIdPrefix + ++_sequence,
                DateTimeOffset.Now);
            if (_pendingReminders.Count < int.MaxValue)
            {
                _pendingReminders.Add(reminder);
            }
        }
        return ToNeuBellItem(reminder);
    }

    public int ConsumeLatest()
    {
        return ConsumeLatestAndGetItem() == null ? 0 : 1;
    }

    public NeuBellItem ConsumeLatestAndGetItem()
    {
        Reminder reminder;
        lock (_syncRoot)
        {
            if (_pendingReminders.Count == 0)
            {
                return null;
            }
            var index = _pendingReminders.Count - 1;
            reminder = _pendingReminders[index];
            _pendingReminders.RemoveAt(index);
        }
        return ToNeuBellItem(reminder);
    }

    public int ConsumeAll()
    {
        return ConsumeAllAndGetItems().Count;
    }

    public IReadOnlyList<NeuBellItem> ConsumeAllAndGetItems()
    {
        Reminder[] reminders;
        lock (_syncRoot)
        {
            reminders = _pendingReminders.ToArray();
            _pendingReminders.Clear();
        }
        return reminders
            .OrderByDescending(item => item.CreatedAt)
            .Select(ToNeuBellItem)
            .ToArray();
    }

    public ValueTask<int> ConsumeItemAsync(
        NeuBellRequestContext context,
        string itemId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot)
        {
            var index = _pendingReminders.FindIndex(item => string.Equals(item.Id, itemId, StringComparison.Ordinal));
            if (index < 0)
            {
                return ValueTask.FromResult(0);
            }
            _pendingReminders.RemoveAt(index);
            return ValueTask.FromResult(1);
        }
    }

    ValueTask<int> INeuBellConsumableProvider.ConsumeAllAsync(
        NeuBellRequestContext context,
        CancellationToken cancellationToken) => ValueTask.FromResult(ConsumeAll());

    public ValueTask<NeuBellSnapshot> GetSnapshotAsync(
        NeuBellRequestContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Reminder> pendingReminders;
        lock (_syncRoot)
        {
            pendingReminders = _pendingReminders.ToArray();
        }

        IReadOnlyList<NeuBellItem> items = pendingReminders
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new NeuBellItem(
                item.Id,
                ItemTitle,
                ItemSummary,
                1,
                ItemSeverity,
                ItemDetailUrl,
                item.CreatedAt))
            .ToArray();

        return ValueTask.FromResult(new NeuBellSnapshot(
            ProviderId,
            ModuleUid,
            "NeuBell 测试",
            "fa fa-bell-o",
            true,
            items));
    }
}
