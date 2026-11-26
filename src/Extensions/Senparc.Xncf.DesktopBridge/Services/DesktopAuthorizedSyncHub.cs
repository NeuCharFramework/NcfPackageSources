/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DesktopAuthorizedSyncHub.cs
    File description: Related implementation.

    Created by: Senparc - 20260726

    Modified by: Senparc - 20260726
    Change description: v0.1.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.12 Localized DesktopBridge pairing and runtime surfaces

----------------------------------------------------------------*/

using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.DesktopBridge.Models;

namespace Senparc.Xncf.DesktopBridge.Services;

public sealed class DesktopAuthorizedSyncHub
{
    private const int ReplayCapacity = 100;
    private const int SubscriberCapacity = 128;

    private readonly object _syncRoot = new();
    private readonly Queue<AuthorizedSyncEntry> _replayBuffer = new();
    private readonly Dictionary<Guid, AuthorizedSyncSubscriber> _subscribers = new();
    private long _sequence;

    /// <summary>
    /// Publishes an authorized synchronization event and delivers it only to subscribers
    /// with matching owner and policy values.
    /// </summary>
    /// <param name="event">The authorized synchronization event.</param>
    public void Publish(IAuthorizedIntegrationSyncEvent @event)
    {
        if (string.IsNullOrWhiteSpace(@event.OwnerId) ||
            string.IsNullOrWhiteSpace(@event.Channel) ||
            string.IsNullOrWhiteSpace(@event.ResourceId) ||
            string.IsNullOrWhiteSpace(@event.RequiredPolicy))
        {
            return;
        }

        var entry = new AuthorizedSyncEntry(
            Interlocked.Increment(ref _sequence),
            @event.OwnerId,
            @event.RequiredPolicy,
            new DesktopAuthorizedSyncMessage(
                Sequence: 0,
                Channel: @event.Channel,
                ResourceId: @event.ResourceId,
                Action: @event.Action,
                Time: new DateTimeOffset(DateTime.SpecifyKind(@event.CreationDate, DateTimeKind.Utc))));
        entry = entry with { Message = entry.Message with { Sequence = entry.Sequence } };

        List<AuthorizedSyncSubscriber> subscribers;
        lock (_syncRoot)
        {
            _replayBuffer.Enqueue(entry);
            while (_replayBuffer.Count > ReplayCapacity)
            {
                _replayBuffer.Dequeue();
            }

            subscribers = _subscribers.Values.ToList();
        }

        foreach (var subscriber in subscribers)
        {
            if (CanRead(entry, subscriber.OwnerId, subscriber.RequiredPolicy))
            {
                subscriber.Channel.Writer.TryWrite(entry);
            }
        }
    }

    /// <summary>
    /// Subscribes to authorized synchronization events filtered by owner and required policy.
    /// </summary>
    /// <param name="ownerId">The owner identity allowed to receive events.</param>
    /// <param name="requiredPolicy">The policy identity required for delivery.</param>
    /// <param name="replayBuffered">Whether retained matching events should be replayed first.</param>
    /// <param name="cancellationToken">Token used to terminate the subscription.</param>
    /// <returns>An asynchronous filtered event sequence.</returns>
    public async IAsyncEnumerable<DesktopAuthorizedSyncMessage> Subscribe(
        string ownerId,
        string requiredPolicy,
        bool replayBuffered = true,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateBounded<AuthorizedSyncEntry>(new BoundedChannelOptions(SubscriberCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        AuthorizedSyncEntry[] buffered;
        lock (_syncRoot)
        {
            buffered = replayBuffered ? _replayBuffer.ToArray() : Array.Empty<AuthorizedSyncEntry>();
            _subscribers[subscriptionId] = new AuthorizedSyncSubscriber(ownerId, requiredPolicy, channel);
        }

        try
        {
            foreach (var item in buffered)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (CanRead(item, ownerId, requiredPolicy))
                {
                    yield return item.Message;
                }
            }

            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                if (CanRead(item, ownerId, requiredPolicy))
                {
                    yield return item.Message;
                }
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

    /// <summary>
    /// Determines whether a buffered sync entry is visible to a subscriber.
    /// </summary>
    /// <param name="entry">The buffered entry.</param>
    /// <param name="ownerId">The subscriber owner identifier.</param>
    /// <param name="requiredPolicy">The subscriber policy.</param>
    /// <returns><see langword="true"/> when both owner and policy match exactly.</returns>
    private static bool CanRead(AuthorizedSyncEntry entry, string ownerId, string requiredPolicy)
    {
        return string.Equals(entry.OwnerId, ownerId, StringComparison.Ordinal) &&
               string.Equals(entry.RequiredPolicy, requiredPolicy, StringComparison.Ordinal);
    }

    private sealed record AuthorizedSyncEntry(
        long Sequence,
        string OwnerId,
        string RequiredPolicy,
        DesktopAuthorizedSyncMessage Message);

    private sealed record AuthorizedSyncSubscriber(
        string OwnerId,
        string RequiredPolicy,
        Channel<AuthorizedSyncEntry> Channel);
}
