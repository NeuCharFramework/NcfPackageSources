/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuBellChangeNotifier.cs
    File description: Related implementation.

    Created by: Senparc - 20260803

    Modified by: Senparc - 20260804
    Change description: v0.3.0 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.3.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Senparc.Ncf.Shared.Abstractions.NeuBell;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class NeuBellChangeNotifier : INeuBellPublisher
{
    private const int SubscriberCapacity = 64;
    private readonly object _syncRoot = new();
    private readonly Dictionary<Guid, Channel<string>> _subscribers = new();
    private readonly ConcurrentDictionary<string, long> _providerRevisions =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Increments a provider revision and publishes its identifier to every current
    /// bounded subscriber. Blank provider IDs are ignored.
    /// </summary>
    /// <param name="providerId">The provider whose state changed.</param>
    /// <param name="cancellationToken">Token checked before publishing.</param>
    /// <returns>A completed value task after the notification is queued.</returns>
    public ValueTask NotifyChangedAsync(string providerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            return ValueTask.CompletedTask;
        }

        cancellationToken.ThrowIfCancellationRequested();
        _providerRevisions.AddOrUpdate(providerId, 1, (_, revision) => revision + 1);

        Channel<string>[] subscribers;
        lock (_syncRoot)
        {
            subscribers = _subscribers.Values.ToArray();
        }

        foreach (var subscriber in subscribers)
        {
            subscriber.Writer.TryWrite(providerId);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Returns the latest in-memory revision observed for a provider.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <returns>The revision, or zero when the provider has not published a change.</returns>
    public long GetRevision(string providerId)
    {
        return _providerRevisions.TryGetValue(providerId, out var revision) ? revision : 0;
    }

    /// <summary>
    /// Subscribes to provider-change notifications until cancellation or disposal,
    /// dropping the oldest queued values when a subscriber is slower than the bounded channel.
    /// </summary>
    /// <param name="cancellationToken">Token used to end the subscription.</param>
    /// <returns>An asynchronous sequence of changed provider identifiers.</returns>
    public async IAsyncEnumerable<string> SubscribeAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(SubscriberCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        lock (_syncRoot)
        {
            _subscribers[subscriptionId] = channel;
        }

        try
        {
            await foreach (var providerId in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return providerId;
            }
        }
        finally
        {
            lock (_syncRoot)
            {
                _subscribers.Remove(subscriptionId);
            }

            channel.Writer.TryComplete();
        }
    }
}
