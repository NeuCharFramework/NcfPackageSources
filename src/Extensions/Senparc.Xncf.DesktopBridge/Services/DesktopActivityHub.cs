/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopActivityHub.cs
    文件功能描述：桌面活动广播、短期回放和活动快照

    创建标识：Senparc - 20260725

    修改标识：Senparc - 20260726
    修改描述：v0.1.0-preview2 同步模块功能与兼容性改进

----------------------------------------------------------------*/

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Senparc.Xncf.DesktopBridge.Models;

namespace Senparc.Xncf.DesktopBridge.Services;

public sealed class DesktopActivityHub
{
    private const int ReplayCapacity = 100;
    private const int SubscriberCapacity = 256;

    private readonly object _syncRoot = new();
    private readonly Queue<DesktopActivityMessage> _replayBuffer = new();
    private readonly Dictionary<Guid, Channel<DesktopActivityMessage>> _subscribers = new();
    private readonly ConcurrentDictionary<string, DesktopActivityMessage> _activeActivities =
        new(StringComparer.OrdinalIgnoreCase);
    private long _sequence;

    public DesktopActivityMessage Publish(DesktopActivityMessage message)
    {
        var sequenced = message with { Sequence = Interlocked.Increment(ref _sequence) };
        List<Channel<DesktopActivityMessage>> subscribers;

        lock (_syncRoot)
        {
            _replayBuffer.Enqueue(sequenced);
            while (_replayBuffer.Count > ReplayCapacity)
            {
                _replayBuffer.Dequeue();
            }

            subscribers = _subscribers.Values.ToList();
        }

        if (sequenced.IsTerminal)
        {
            _activeActivities.TryRemove(sequenced.ActivityId, out _);
        }
        else
        {
            _activeActivities[sequenced.ActivityId] = sequenced;
        }

        foreach (var subscriber in subscribers)
        {
            subscriber.Writer.TryWrite(sequenced);
        }

        return sequenced;
    }

    public IReadOnlyList<DesktopActivityMessage> GetActiveSnapshot()
    {
        return _activeActivities.Values
            .OrderByDescending(z => z.Time)
            .ToArray();
    }

    public async IAsyncEnumerable<DesktopActivityMessage> Subscribe(
        bool replayBuffered = true,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateBounded<DesktopActivityMessage>(new BoundedChannelOptions(SubscriberCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        DesktopActivityMessage[] buffered;
        lock (_syncRoot)
        {
            buffered = replayBuffered ? _replayBuffer.ToArray() : Array.Empty<DesktopActivityMessage>();
            _subscribers[subscriptionId] = channel;
        }

        try
        {
            foreach (var item in buffered)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }

            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return item;
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
